using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CloudTask.Api.Common;
using CloudTask.Api.Services;
using CloudTask.Domain.Entities;
using CloudTask.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CloudTask.Tests.Api;

public class ErrorHandlingTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private HttpClient _client = null!;

    public ErrorHandlingTests(ApiFactory factory)
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

    private sealed class FakeUser : ICurrentUser
    {
        public FakeUser(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }

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
        var anonymous = _factory.CreateClient();
        var email = $"{Guid.NewGuid()}@example.com";
        var body = new { email, displayName = "A", password = "Str0ngPassword" };

        await anonymous.PostAsJsonAsync("/api/auth/register", body);
        var response = await anonymous.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
    }

    [Fact]
    public async Task TaskService_CompleteTwice_ThrowsConflictException()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var db = new AppDbContext(options);
        var user = new User { Email = "a@b.com", DisplayName = "A" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project { Name = "P", OwnerId = user.Id };
        var task = project.AddTask("T");
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var service = new TaskService(db, new FakeUser(user.Id));
        await service.CompleteAsync(project.Id, task.Id);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CompleteAsync(project.Id, task.Id));
    }
}