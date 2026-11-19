using Microsoft.AspNetCore.Identity;

namespace CloudTask.Api.Common;

public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var name in new[] { RoleNames.Admin, RoleNames.Member })
        {
            if (!await roles.RoleExistsAsync(name))
            {
                await roles.CreateAsync(new IdentityRole(name));
            }
        }
    }
}