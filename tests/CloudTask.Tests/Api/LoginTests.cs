using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CloudTask.Api.Dtos;

namespace CloudTask.Tests.Api;

public class LoginTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private const string Password = "Str0ngPassword";

    public LoginTests(ApiFactory factory) => _client = factory.CreateClient();

    private async Task<string> RegisterAsync()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { email, displayName = "Tester", password = Password });
        response.EnsureSuccessStatusCode();
        return email;
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var email = await RegisterAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal(2, body.Token.Count(c => c == '.'));
    }

    [Fact]
    public async Task Login_WrongPassword_And_UnknownEmail_Return401()
    {
        var email = await RegisterAsync();

        var wrong = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });
        var unknown = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@example.com", password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsProfile()
    {
        var email = await RegisterAsync();
        var login = await (await _client.PostAsJsonAsync("/api/auth/login", new { email, password = Password }))
            .Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(email, me!.Email);
    }

    [Fact]
    public async Task Me_WithTamperedToken_Returns401()
    {
        var email = await RegisterAsync();
        var login = await (await _client.PostAsJsonAsync("/api/auth/login", new { email, password = Password }))
            .Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token + "x");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}