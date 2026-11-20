
using CloudTask.Api.Dtos;
using CloudTask.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;

namespace CloudTask.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Registers a new user with a password.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        var created = await _auth.RegisterAsync(request);

        return CreatedAtAction(
            nameof(UsersController.GetById),
            "Users",
            new { id = created.Id },
            created);
    }

    /// <summary>Logs in and returns a JWT.</summary>
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        return Ok(await _auth.LoginAsync(request));
    }

    /// <summary>Returns the current user's profile. Requires a valid token.</summary>
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Me([FromServices] IUserService users)
    {
        var id = int.Parse(User.FindFirst("sub")!.Value);

        return Ok(await users.GetByIdAsync(id));
    }
}
