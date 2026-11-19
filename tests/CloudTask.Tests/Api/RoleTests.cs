using System.Net;

namespace CloudTask.Tests.Api;

public class RoleTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RoleTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Member_CannotCallAdminEndpoint_Returns403()
    {
        var member = await _factory.CreateAuthenticatedClientAsync();

        var response = await member.GetAsync("/api/users/admin/all");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCallAdminEndpoint_Returns200()
    {
        var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/users/admin/all");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCallAdminEndpoint_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/users/admin/all");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}