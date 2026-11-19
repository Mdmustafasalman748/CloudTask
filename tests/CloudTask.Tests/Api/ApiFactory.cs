
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CloudTask.Api.Dtos;
using CloudTask.Domain.Entities;
using CloudTask.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
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

    public static bool UsingSqlServer =>
        !string.IsNullOrWhiteSpace(BaseConnection);

    private readonly string _dbName = Guid.NewGuid().ToString("N");

    private string SqlConnectionString()
    {
        var csb = new SqlConnectionStringBuilder(BaseConnection)
        {
            InitialCatalog = "CloudTaskTests_" + _dbName
        };

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
            options.ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connection = UsingSqlServer
            ? SqlConnectionString()
            : "Server=(localdb)\\MSSQLLocalDB;Database=unused;Trusted_Connection=True;";

        builder.UseSetting("ConnectionStrings:DefaultConnection", connection);
        builder.UseSetting("Jwt:Key", "test-only-signing-key-0123456789abcdefghij");
        builder.UseSetting("Jwt:Issuer", "CloudTask");
        builder.UseSetting("Jwt:Audience", "CloudTask.Clients");
        builder.UseSetting("Jwt:ExpiryMinutes", "60");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(ConfigureDb);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        if (UsingSqlServer)
        {
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
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
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
            }
            catch
            {
                // Best-effort cleanup of the isolated test database.
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

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"{Guid.NewGuid()}@example.com";

        // Identity's default password policy requires a non-alphanumeric character.
        const string password = "Str0ng!Password";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                displayName = "Tester",
                password
            });

        if (!register.IsSuccessStatusCode)
        {
            var error = await register.Content.ReadAsStringAsync();

            client.Dispose();

            throw new InvalidOperationException(
                $"Test registration failed with {(int)register.StatusCode} " +
                $"{register.StatusCode}. Response: {error}");
        }

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });

        if (!login.IsSuccessStatusCode)
        {
            var error = await login.Content.ReadAsStringAsync();

            client.Dispose();

            throw new InvalidOperationException(
                $"Test login failed with {(int)login.StatusCode} " +
                $"{login.StatusCode}. Response: {error}");
        }

        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        if (body is null || string.IsNullOrWhiteSpace(body.Token))
        {
            client.Dispose();

            throw new InvalidOperationException(
                "Login succeeded, but the response did not contain a JWT token.");
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.Token);

        return client;
    }
}
