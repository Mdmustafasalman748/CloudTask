using System.Net;
using System.Net.Http.Json;
using CloudTask.Api.Dtos;

namespace CloudTask.Tests.Api;

public class ProjectsEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ProjectsEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<ProjectResponse> CreateProjectAsync(string name = "Demo")
    {
        var ownerId = await _factory.SeedUserAsync();
        var response = await _client.PostAsJsonAsync("/api/projects",
            new { name, description = "Test", ownerId });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>())!;
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocation()
    {
        var ownerId = await _factory.SeedUserAsync();

        var response = await _client.PostAsJsonAsync("/api/projects",
            new { name = "Demo", description = "First", ownerId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.Equal("Demo", body!.Name);
        Assert.Equal(ownerId, body.OwnerId);
    }

    [Fact]
    public async Task Create_EmptyName_Returns400()
    {
        var ownerId = await _factory.SeedUserAsync();

        var response = await _client.PostAsJsonAsync("/api/projects",
            new { name = "", ownerId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownOwner_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/projects",
            new { name = "Demo", ownerId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/projects/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesName()
    {
        var created = await CreateProjectAsync("Old name");

        var put = await _client.PutAsJsonAsync($"/api/projects/{created.Id}",
            new { name = "New name", description = "Updated" });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var fetched = await _client.GetFromJsonAsync<ProjectResponse>($"/api/projects/{created.Id}");
        Assert.Equal("New name", fetched!.Name);
    }

    [Fact]
    public async Task Delete_RemovesProject()
    {
        var created = await CreateProjectAsync();

        var delete = await _client.DeleteAsync($"/api/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await _client.GetAsync($"/api/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}