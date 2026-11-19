namespace CloudTask.Api.Common;

public interface ICurrentUser
{
    int Id { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public int Id
    {
        get
        {
            var sub = _accessor.HttpContext?.User.FindFirst("sub")?.Value;

            if (!int.TryParse(sub, out var id))
            {
                throw new UnauthorizedException("Not authenticated.");
            }

            return id;
        }
    }
}