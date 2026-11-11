using System.Net;
using System.Net.Http.Json;
using CloudTask.Api.Dtos;

namespace CloudTask.Tests.Api;

public class UsersEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public UsersEndpointTests(ApiFactory factory) => _client = factory.CreateClient();

    private static string NewEmail() => $"{Guid.NewGuid()}@example.com";

    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        var email = NewEmail();

        var response = await _client.PostAsJsonAsync("/api/users",
            new { email, displayName = "Alice" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(email, body!.Email);
    }

    [Fact]
    public async Task Create_NormalizesEmailCase()
    {
        var response = await _client.PostAsJsonAsync("/api/users",
            new { email = $"  {Guid.NewGuid()}@EXAMPLE.com ", displayName = "Bob" });

        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(body!.Email, body.Email.ToLowerInvariant());
        Assert.Equal(body.Email.Trim(), body.Email);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns409()
    {
        var email = NewEmail();
        await _client.PostAsJsonAsync("/api/users", new { email, displayName = "First" });

        var second = await _client.PostAsJsonAsync("/api/users",
            new { email = email.ToUpperInvariant(), displayName = "Second" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/users",
            new { email = "not-an-email", displayName = "Carol" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/users/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}