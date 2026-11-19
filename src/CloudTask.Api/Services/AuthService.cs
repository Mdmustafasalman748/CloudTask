using CloudTask.Api.Common;
using CloudTask.Api.Dtos;
using CloudTask.Domain.Entities;
using CloudTask.Infrastructure.Identity;
using CloudTask.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CloudTask.Api.Services;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly ITokenService _tokens;

    public AuthService(
        AppDbContext db, UserManager<AppIdentityUser> userManager, ITokenService tokens)
    {
        _db = db;
        _userManager = userManager;
        _tokens = tokens;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            throw new ConflictException($"A user with email '{email}' already exists.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var profile = new User { Email = email, DisplayName = request.DisplayName.Trim() };
        _db.Users.Add(profile);
        await _db.SaveChangesAsync();

        var identity = new AppIdentityUser
        {
            UserName = email,
            Email = email,
            UserId = profile.Id
        };

        var result = await _userManager.CreateAsync(identity, request.Password);
        if (!result.Succeeded)
        {
            throw new BadRequestException(
                string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        // Everyone registers as a Member; Admins are promoted deliberately, never here
        var roleResult = await _userManager.AddToRoleAsync(identity, RoleNames.Member);
        if (!roleResult.Succeeded)
        {
            throw new BadRequestException(
                string.Join(" ", roleResult.Errors.Select(e => e.Description)));
        }

        await tx.CommitAsync();

        return new UserResponse(profile.Id, profile.Email, profile.DisplayName, profile.CreatedAt);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var identity = await _userManager.FindByEmailAsync(email);

        const string failureMessage = "Invalid email or password.";

        if (identity is null)
        {
            throw new UnauthorizedException(failureMessage);
        }

        if (await _userManager.IsLockedOutAsync(identity))
        {
            throw new UnauthorizedException(failureMessage);
        }

        if (!await _userManager.CheckPasswordAsync(identity, request.Password))
        {
            await _userManager.AccessFailedAsync(identity);
            throw new UnauthorizedException(failureMessage);
        }

        await _userManager.ResetAccessFailedCountAsync(identity);

        var profile = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == identity.UserId);
        var roles = await _userManager.GetRolesAsync(identity);
        var (token, expiresAt) = _tokens.Create(identity, roles);

        return new LoginResponse(
            token,
            expiresAt,
            new UserResponse(profile.Id, profile.Email, profile.DisplayName, profile.CreatedAt));
    }
}