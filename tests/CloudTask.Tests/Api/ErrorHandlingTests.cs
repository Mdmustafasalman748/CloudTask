using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CloudTask.Api.Common;
using CloudTask.Api.Services;
using CloudTask.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudTask.Tests.Api;

public class ErrorHandlingTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ErrorHandlingTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task NotFound_ReturnsProblemDetails()
    {
        var response = await _client.GetAsync("/api/projects/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType!.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem!.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Contains("999999", problem.Detail);
    }

    [Fact]
    public async Task Conflict_ReturnsProblemDetailsWithDomainMessage()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/users", new { email, displayName = "A" });

        var response = await _client.PostAsJsonAsync("/api/users", new { email, displayName = "B" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
    }

    [Fact]
    public async Task TaskService_CompleteTwice_ThrowsConflictException()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        var user = new CloudTask.Domain.Entities.User { Email = "a@b.com", DisplayName = "A" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new CloudTask.Domain.Entities.Project { Name = "P", OwnerId = user.Id };
        var task = project.AddTask("T");
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var service = new TaskService(db);
        await service.CompleteAsync(project.Id, task.Id);

        await Assert.ThrowsAsync<ConflictException>(() => service.CompleteAsync(project.Id, task.Id));
    }
}