using CloudTask.Domain.Entities;
using CloudTask.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CloudTask.Tests.Api;

public class ApiFactory : WebApplicationFactory<Program>
{
    private static readonly string? BaseConnection =
        Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION");

    public static bool UsingSqlServer => !string.IsNullOrWhiteSpace(BaseConnection);

    private readonly string _dbName = Guid.NewGuid().ToString("N");

    private string SqlConnectionString() =>
        new SqlConnectionStringBuilder(BaseConnection)
        {
            InitialCatalog = $"CloudTaskTests_{_dbName}"
        }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs requires a connection string to exist
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            UsingSqlServer ? SqlConnectionString() : "Server=unused;Database=unused");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            if (UsingSqlServer)
                services.AddDbContext<AppDbContext>(o => o.UseSqlServer(SqlConnectionString()));
            else
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    // Runs once the host is built: create the schema from your real migrations
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        if (UsingSqlServer)
        {
            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && UsingSqlServer)
        {
            try
            {
                using var scope = Services.CreateScope();
                scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeleted();
            }
            catch
            {
                // Cleanup is best-effort; a leftover test database is harmless
            }
        }

        base.Dispose(disposing);
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