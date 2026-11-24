using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CloudTask.Web.Models;

namespace CloudTask.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<LoginResultDto?> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoginResultDto>();
    }

    public Task<ApiResult> RegisterAsync(string email, string displayName, string password)
    {
        return SendAsync(HttpMethod.Post, "api/auth/register", null,
            new { email, displayName, password });
    }

    public Task<ApiResult<PagedDto<ProjectDto>>> GetProjectsAsync(string token)
    {
        return SendAsync<PagedDto<ProjectDto>>(
            HttpMethod.Get, "api/projects?pageSize=50", token, null);
    }

    public Task<ApiResult<ProjectDetailDto>> GetProjectAsync(string token, int id)
    {
        return SendAsync<ProjectDetailDto>(HttpMethod.Get, $"api/projects/{id}", token, null);
    }

    public Task<ApiResult<ProjectDetailDto>> CreateProjectAsync(
        string token, string name, string? description)
    {
        return SendAsync<ProjectDetailDto>(
            HttpMethod.Post, "api/projects", token, new { name, description });
    }

    public Task<ApiResult<PagedDto<TaskDto>>> GetTasksAsync(string token, int projectId)
    {
        return SendAsync<PagedDto<TaskDto>>(
            HttpMethod.Get, $"api/projects/{projectId}/tasks?pageSize=100", token, null);
    }

    public Task<ApiResult<TaskDto>> CreateTaskAsync(
        string token, int projectId, string title, string priority)
    {
        return SendAsync<TaskDto>(
            HttpMethod.Post, $"api/projects/{projectId}/tasks", token, new { title, priority });
    }

    public Task<ApiResult<TaskDto>> TaskActionAsync(
        string token, int projectId, int taskId, string action)
    {
        return SendAsync<TaskDto>(
            HttpMethod.Post, $"api/projects/{projectId}/tasks/{taskId}/{action}", token, null);
    }

    private async Task<ApiResult> SendAsync(
        HttpMethod method, string url, string? token, object? body)
    {
        var response = await SendRawAsync(method, url, token, body);
        return new ApiResult
        {
            Success = response.IsSuccessStatusCode,
            StatusCode = (int)response.StatusCode,
            Error = response.IsSuccessStatusCode ? null : await ReadErrorAsync(response)
        };
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method, string url, string? token, object? body)
    {
        var response = await SendRawAsync(method, url, token, body);

        if (!response.IsSuccessStatusCode)
        {
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = (int)response.StatusCode,
                Error = await ReadErrorAsync(response)
            };
        }

        return new ApiResult<T>
        {
            Success = true,
            StatusCode = (int)response.StatusCode,
            Data = await response.Content.ReadFromJsonAsync<T>()
        };
    }

    private async Task<HttpResponseMessage> SendRawAsync(
        HttpMethod method, string url, string? token, object? body)
    {
        var request = new HttpRequestMessage(method, url);

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _http.SendAsync(request);
    }

    // Pulls the "detail" text out of a ProblemDetails response, with a safe fallback
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.TryGetProperty("detail", out var detail)
                    && detail.ValueKind == JsonValueKind.String)
                {
                    return detail.GetString()!;
                }

                if (doc.RootElement.TryGetProperty("errors", out var errors))
                {
                    return string.Join(" ", errors.EnumerateObject()
                        .SelectMany(e => e.Value.EnumerateArray().Select(v => v.GetString())));
                }
            }
        }
        catch (JsonException)
        {
        }

        return $"The request failed ({(int)response.StatusCode}).";
    }
}