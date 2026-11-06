using CloudTask.Domain.Entities;
using CloudTask.Domain.Enums;
using CloudTask.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;

namespace CloudTask.Tests.Infrastructure;

public class AppDbContextTests
{
    private static DbContextOptions<AppDbContext> NewOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task CanSaveAndLoadProjectWithAssignedTask()
    {
        var options = NewOptions();
        int userId;

        // Arrange + save using one context
        await using (var context = new AppDbContext(options))
        {
            var user = new User { Email = "dev@example.com", DisplayName = "Dev" };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            userId = user.Id;

            var project = new Project { Name = "Demo", OwnerId = user.Id };
            var task = project.AddTask("Write docs", TaskPriority.High);
            task.AssignTo(user);
            task.Start();

            context.Projects.Add(project);
            await context.SaveChangesAsync();
        }

        // Load using a fresh context so we know the data was really stored
        await using (var context = new AppDbContext(options))
        {
            var loaded = await context.Projects
                .Include(p => p.Tasks)
                .SingleAsync();

            var loadedTask = Assert.Single(loaded.Tasks);
            Assert.Equal("Write docs", loadedTask.Title);
            Assert.Equal(TaskItemStatus.InProgress, loadedTask.Status);
            Assert.Equal(userId, loadedTask.AssigneeId);
        }
    }
}