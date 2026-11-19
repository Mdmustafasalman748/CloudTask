using System.Net;
using System.Net.Http.Json;
using CloudTask.Api.Common;
using CloudTask.Api.Dtos;

namespace CloudTask.Tests.Api;

public class TasksEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private HttpClient _client = null!;

    public TasksEndpointTests(ApiFactory factory)
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

    private async Task<int> CreateProjectAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name = "P" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>())!.Id;
    }

    private async Task<TaskResponse> CreateTaskAsync(int projectId, string title = "Write docs")
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/projects/{projectId}/tasks", new { title, priority = "High" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>())!;
    }

    [Fact]
    public async Task Create_ReturnsTodoTaskWithPriority()
    {
        var projectId = await CreateProjectAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/projects/{projectId}/tasks", new { title = "Write docs", priority = "High" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.Equal("Todo", body!.Status);
        Assert.Equal("High", body.Priority);
        Assert.Equal(projectId, body.ProjectId);
    }

    [Fact]
    public async Task Create_UnknownProject_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/projects/999999/tasks", new { title = "Orphan" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_EmptyTitle_Returns400()
    {
        var projectId = await CreateProjectAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/projects/{projectId}/tasks", new { title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StartThenComplete_MovesThroughStatuses()
    {
        var projectId = await CreateProjectAsync();
        var task = await CreateTaskAsync(projectId);

        var start = await _client.PostAsync(
            $"/api/projects/{projectId}/tasks/{task.Id}/start", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        Assert.Equal("InProgress", (await start.Content.ReadFromJsonAsync<TaskResponse>())!.Status);

        var complete = await _client.PostAsync(
            $"/api/projects/{projectId}/tasks/{task.Id}/complete", null);
        var done = await complete.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.Equal("Done", done!.Status);
        Assert.NotNull(done.CompletedAt);
    }

    [Fact]
    public async Task Complete_Twice_Returns409()
    {
        var projectId = await CreateProjectAsync();
        var task = await CreateTaskAsync(projectId);
        await _client.PostAsync($"/api/projects/{projectId}/tasks/{task.Id}/complete", null);

        var second = await _client.PostAsync(
            $"/api/projects/{projectId}/tasks/{task.Id}/complete", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Assign_SetsAssignee_AndRejectsUnknownUser()
    {
        var projectId = await CreateProjectAsync();
        var task = await CreateTaskAsync(projectId);
        var userId = await _factory.SeedUserAsync();

        var ok = await _client.PostAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{task.Id}/assign", new { userId });
        Assert.Equal(userId, (await ok.Content.ReadFromJsonAsync<TaskResponse>())!.AssigneeId);

        var bad = await _client.PostAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{task.Id}/assign", new { userId = 999999 });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task GetAll_FiltersByStatus_AndScopesToProject()
    {
        var projectId = await CreateProjectAsync();
        var otherProjectId = await CreateProjectAsync();
        var started = await CreateTaskAsync(projectId, "A");
        await CreateTaskAsync(projectId, "B");
        await CreateTaskAsync(otherProjectId, "Other project's task");
        await _client.PostAsync($"/api/projects/{projectId}/tasks/{started.Id}/start", null);

        var inProgress = await _client.GetFromJsonAsync<PagedResult<TaskResponse>>(
            $"/api/projects/{projectId}/tasks?status=InProgress");
        var all = await _client.GetFromJsonAsync<PagedResult<TaskResponse>>(
            $"/api/projects/{projectId}/tasks");

        Assert.Single(inProgress!.Items);
        Assert.Equal(2, all!.Items.Count);
        Assert.All(all.Items, t => Assert.Equal(projectId, t.ProjectId));
    }

    [Fact]
    public async Task WrongProject_Returns404_ForGetAndStart()
    {
        var projectA = await CreateProjectAsync();
        var projectB = await CreateProjectAsync();
        var task = await CreateTaskAsync(projectA);

        var get = await _client.GetAsync($"/api/projects/{projectB}/tasks/{task.Id}");
        var start = await _client.PostAsync(
            $"/api/projects/{projectB}/tasks/{task.Id}/start", null);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, start.StatusCode);
    }
}