using System.Net;
using System.Threading.Tasks;

namespace CloudTask.Tests.Api;

public class UsersEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private HttpClient _client = null!;

    public UsersEndpointTests(ApiFactory factory)
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

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/users/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}