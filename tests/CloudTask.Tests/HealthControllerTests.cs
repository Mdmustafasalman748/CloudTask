using CloudTask.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace CloudTask.Tests;

public class HealthControllerTests
{
    [Fact]
    public void Get_ReturnsOkResult()
    {
        var controller = new HealthController();

        var result = controller.Get();

        Assert.IsType<OkObjectResult>(result);
    }
}