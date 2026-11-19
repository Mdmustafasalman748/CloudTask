using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CloudTask.Api.Dtos;
using CloudTask.Domain.Entities;
using CloudTask.Infrastructure.Identity;
using CloudTask.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CloudTask.Tests.Api;

public class ApiFactory : WebApplicationFactory<Program>
{
    private static readonly string? BaseConnection =
        Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION");

    public static bool UsingSqlServer
    {
        get { return !string.IsNullOrWhiteSpace(BaseConnection); }
    }

    private readonly string _dbName = Guid.NewGuid().ToString("N");

    private string SqlConnectionString()
    {
        var csb = new SqlConnectionStringBuilder(BaseConnection);
        csb.InitialCatalog = "CloudTaskTests_" + _dbName;
        return csb.ConnectionString;
    }

    private void ConfigureDb(DbContextOptionsBuilder options)
    {
        if (UsingSqlServer)
        {
            options.UseSqlServer(SqlConnectionString());
        }
        else
        {
            options.UseInMemoryDatabase(_dbName);
            options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        string connection = "Server=unused;Database=unused";
        if (UsingSqlServer)
        {
            connection = SqlConnectionString();

            // Create the schema BEFORE Program.cs runs, because Program.cs seeds roles at startup
            var migrateOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connection)
                .Options;
            using (var db = new AppDbContext(migrateOptions))
            {
                db.Database.Migrate();
            }
        }

        builder.UseSetting("ConnectionStrings:DefaultConnection", connection);
        builder.UseSetting("Jwt:Key", "test-only-signing-key-0123456789abcdefghij");
        builder.UseSetting("Jwt:Issuer", "CloudTask");
        builder.UseSetting("Jwt:Audience", "CloudTask.Clients");
        builder.UseSetting("Jwt:ExpiryMinutes", "60");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => ConfigureDb(options));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && UsingSqlServer)
        {
            try
            {
                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
            }
            catch
            {
                // Best-effort cleanup
            }
        }

        base.Dispose(disposing);
    }

    // Inserts a domain profile directly (useful for task assignees)
    public async Task<int> SeedUserAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User();
        user.Email = Guid.NewGuid() + "@example.com";
        user.DisplayName = "Test User";

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    // Registers a fresh Member, logs in, and returns a client that sends the Bearer token
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = Guid.NewGuid() + "@example.com";
        const string password = "Str0ngPassword";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, displayName = "Tester", password });
        register.EnsureSuccessStatusCode();

        await LoginAsync(client, email, password);
        return client;
    }

    // Registers a user, promotes them to Admin, then logs in (so the token carries the role)
    public async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateClient();
        var email = Guid.NewGuid() + "@example.com";
        const string password = "Str0ngPassword";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, displayName = "Admin", password });
        register.EnsureSuccessStatusCode();

        using (var scope = Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppIdentityUser>>();
            var identity = await users.FindByEmailAsync(email);
            await users.AddToRoleAsync(identity!, "Admin");
        }

        await LoginAsync(client, email, password);
        return client;
    }

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.Token);
    }
}