using System.Net;
using System.Net.Http.Json;
using CloudTask.Api.Common;
using CloudTask.Api.Dtos;

namespace CloudTask.Tests.Api;

public class TasksPagingTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private HttpClient _client = null!;

    public TasksPagingTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task<int> CreateProjectAsync(string name = "P")
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>())!.Id;
    }

    private async Task<TaskResponse> CreateTaskAsync(int projectId, string title)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/projects/{projectId}/tasks", new { title });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>())!;
    }

    [Fact]
    public async Task ProjectTasks_PagingReturnsSliceAndTotals()
    {
        var projectId = await CreateProjectAsync();
        for (var i = 1; i <= 5; i++)
        {
            await CreateTaskAsync(projectId, $"task-{i:D2}");
        }

        var page2 = await _client.GetFromJsonAsync<PagedResult<TaskResponse>>(
            $"/api/projects/{projectId}/tasks?page=2&pageSize=2&sortBy=title");

        Assert.Equal(2, page2!.Items.Count);
        Assert.Equal(5, page2.TotalCount);
        Assert.Equal(3, page2.TotalPages);
        Assert.Equal("task-03", page2.Items[0].Title);
    }

    [Fact]
    public async Task ProjectTasks_StatusFilterCombinesWithPaging()
    {
        var projectId = await CreateProjectAsync();
        for (var i = 1; i <= 3; i++)
        {
            var t = await CreateTaskAsync(projectId, $"t{i}");
            await _client.PostAsync($"/api/projects/{projectId}/tasks/{t.Id}/start", null);
        }
        await CreateTaskAsync(projectId, "still-todo");

        var result = await _client.GetFromJsonAsync<PagedResult<TaskResponse>>(
            $"/api/projects/{projectId}/tasks?status=InProgress&pageSize=2");

        Assert.Equal(3, result!.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, t => Assert.Equal("InProgress", t.Status));
    }

    [Fact]
    public async Task AssignedTasks_SpanProjects_AndIncludeProjectName()
    {
        // The logged-in user is the assignee, because users may only view their own assigned tasks
        var me = await _client.GetFromJsonAsync<UserResponse>("/api/auth/me");
        var userId = me!.Id;

        var projectA = await CreateProjectAsync("Alpha");
        var projectB = await CreateProjectAsync("Beta");

        var taskA = await CreateTaskAsync(projectA, "a-task");
        var taskB = await CreateTaskAsync(projectB, "b-task");
        await CreateTaskAsync(projectA, "unassigned");

        var assignA = await _client.PostAsJsonAsync(
            $"/api/projects/{projectA}/tasks/{taskA.Id}/assign", new { userId });
        assignA.EnsureSuccessStatusCode();

        var assignB = await _client.PostAsJsonAsync(
            $"/api/projects/{projectB}/tasks/{taskB.Id}/assign", new { userId });
        assignB.EnsureSuccessStatusCode();

        var result = await _client.GetFromJsonAsync<PagedResult<AssignedTaskResponse>>(
            $"/api/users/{userId}/tasks");

        Assert.Equal(2, result!.TotalCount);
        Assert.Contains(result.Items, t => t.ProjectName == "Alpha" && t.Title == "a-task");
        Assert.Contains(result.Items, t => t.ProjectName == "Beta" && t.Title == "b-task");
    }

    [Fact]
    public async Task AssignedTasks_UnknownUser_Returns404()
    {
        var response = await _client.GetAsync("/api/users/999999/tasks");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PageSizeAboveCap_Returns400()
    {
        var projectId = await CreateProjectAsync();

        var response = await _client.GetAsync($"/api/projects/{projectId}/tasks?pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }   
}