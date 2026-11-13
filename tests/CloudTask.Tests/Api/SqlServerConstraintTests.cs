using CloudTask.Domain.Entities;
using CloudTask.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CloudTask.Tests.Api;

// These verify database-level rules, so they only run when TEST_SQL_CONNECTION is set.
// Without it they return immediately and show as passed.
public class SqlServerConstraintTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SqlServerConstraintTests(ApiFactory factory) => _factory = factory;

    private static AppDbContext Db(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task UniqueIndex_RejectsDuplicateEmail()
    {
        if (!ApiFactory.UsingSqlServer) return;

        using var scope = _factory.Services.CreateScope();
        var db = Db(scope);
        var email = $"{Guid.NewGuid()}@example.com";

        db.Users.Add(new User { Email = email, DisplayName = "A" });
        await db.SaveChangesAsync();

        db.Users.Add(new User { Email = email, DisplayName = "B" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletingProject_CascadesToTasks_InTheDatabase()
    {
        if (!ApiFactory.UsingSqlServer) return;

        using var scope = _factory.Services.CreateScope();
        var db = Db(scope);

        var user = new User { Email = $"{Guid.NewGuid()}@example.com", DisplayName = "A" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var project = new Project { Name = "P", OwnerId = user.Id };
        project.AddTask("T1");
        project.AddTask("T2");
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        var projectId = project.Id;

        // ExecuteDelete bypasses EF's change tracker, so only the DATABASE can cascade
        await db.Projects.Where(p => p.Id == projectId).ExecuteDeleteAsync();

        Assert.Equal(0, await db.Tasks.CountAsync(t => t.ProjectId == projectId));
    }

    [Fact]
    public async Task DeletingUser_WhoOwnsProjects_IsRestricted()
    {
        if (!ApiFactory.UsingSqlServer) return;

        int userId;
        using (var setup = _factory.Services.CreateScope())
        {
            var db = Db(setup);
            var user = new User { Email = $"{Guid.NewGuid()}@example.com", DisplayName = "Owner" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            db.Projects.Add(new Project { Name = "P", OwnerId = user.Id });
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        // A fresh context knows nothing about the project, so the database must reject it
        using var scope = _factory.Services.CreateScope();
        var fresh = Db(scope);
        var toDelete = await fresh.Users.SingleAsync(u => u.Id == userId);
        fresh.Users.Remove(toDelete);

        await Assert.ThrowsAsync<DbUpdateException>(() => fresh.SaveChangesAsync());
    }
}