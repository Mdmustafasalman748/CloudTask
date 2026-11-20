using System.Net;
using System.Net.Http.Json;
using CloudTask.Api.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CloudTask.Tests.Api;

public class HardeningTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public HardeningTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Responses_IncludeSecurityHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task Login_IsRateLimited_Returns429()
    {
        // A separate factory with a tiny limit, so the shared one stays permissive
        var limited = _factory.WithWebHostBuilder(b =>
            b.UseSetting("RateLimiting:LoginPerMinute", "3"));
        var client = limited.CreateClient();

        HttpStatusCode last = HttpStatusCode.OK;
        for (var i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login", new { email = "x@example.com", password = "WrongPassword1" });
            last = response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last);
    }
}