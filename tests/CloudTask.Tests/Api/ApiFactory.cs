using CloudTask.Domain.Entities;
using CloudTask.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CloudTask.Tests.Api;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs requires a connection string; this dummy is never used to connect
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused;Database=unused");

        builder.ConfigureServices(services =>
        {
            // Swap SQL Server for an in-memory database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    public async Task<int> SeedUserAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Email = $"{Guid.NewGuid()}@example.com",
            DisplayName = "Test User"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }
}