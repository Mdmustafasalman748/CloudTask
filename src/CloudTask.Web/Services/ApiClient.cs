using System.Net;
using System.Net.Http.Headers;
using CloudTask.Web.Models;

namespace CloudTask.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    // Returns null when the API answers 401 (wrong email or password)
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

    // Returns null when the token is rejected (expired or invalid)
    public async Task<PagedDto<ProjectDto>?> GetProjectsAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/projects?pageSize=50");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedDto<ProjectDto>>();
    }
}