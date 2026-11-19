using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CloudTask.Api.Dtos;

namespace CloudTask.Tests.Api;

public class AuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthorizationTests(ApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/projects")]
    [InlineData("/api/users")]
    [InlineData("/api/projects/1/tasks")]
    public async Task AnonymousRequests_Return401(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_UsesTokenUserAsOwner()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var me = await client.GetFromJsonAsync<UserResponse>("/api/auth/me");

        var response = await client.PostAsJsonAsync("/api/projects", new { name = "Mine" });
        var project = await response.Content.ReadFromJsonAsync<ProjectResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(me!.Id, project!.OwnerId);
    }

    [Fact]
    public async Task OtherUser_CannotUpdateDeleteOrAddTasks_Returns403()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var intruder = await _factory.CreateAuthenticatedClientAsync();
        var created = await (await owner.PostAsJsonAsync("/api/projects", new { name = "Private" }))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var update = await intruder.PutAsJsonAsync($"/api/projects/{created!.Id}", new { name = "Hacked" });
        var delete = await intruder.DeleteAsync($"/api/projects/{created.Id}");
        var addTask = await intruder.PostAsJsonAsync($"/api/projects/{created.Id}/tasks", new { title = "Sneaky" });

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, addTask.StatusCode);
    }

    [Fact]
    public async Task Owner_CanUpdateAndDelete()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync();
        var created = await (await owner.PostAsJsonAsync("/api/projects", new { name = "Mine" }))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var update = await owner.PutAsJsonAsync($"/api/projects/{created!.Id}", new { name = "Renamed" });
        var delete = await owner.DeleteAsync($"/api/projects/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }
}