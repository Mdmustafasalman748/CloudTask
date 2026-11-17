using Microsoft.AspNetCore.Identity;

namespace CloudTask.Infrastructure.Identity;

public class AppIdentityUser : IdentityUser
{
    public int UserId { get; set; }
}
