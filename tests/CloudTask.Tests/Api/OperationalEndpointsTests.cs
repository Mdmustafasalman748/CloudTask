using System.Net;
using System.Threading.Tasks;

namespace CloudTask.Tests.Api;

public class OperationalEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public OperationalEndpointsTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Live_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_ReturnsOk_WhenDatabaseReachable()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cors_AllowedOrigin_GetsHeader_UnknownOrigin_DoesNot()
    {
        var allowed = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        allowed.Headers.Add("Origin", "http://localhost:5000");
        var denied = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        denied.Headers.Add("Origin", "https://evil.example");

        var a = await _client.SendAsync(allowed);
        var d = await _client.SendAsync(denied);

        Assert.True(a.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(d.Headers.Contains("Access-Control-Allow-Origin"));
    }
}