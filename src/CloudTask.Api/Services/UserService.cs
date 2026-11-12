using CloudTask.Api.Common;
using CloudTask.Api.Dtos;
using CloudTask.Domain.Entities;
using CloudTask.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudTask.Api.Services;

public interface IUserService
{
    Task<List<UserResponse>> GetAllAsync();
    Task<UserResponse> GetByIdAsync(int id);
    Task<UserResponse> CreateAsync(CreateUserRequest request);
}

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db) => _db = db;

    public Task<List<UserResponse>> GetAllAsync() =>
        _db.Users.AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UserResponse(u.Id, u.Email, u.DisplayName, u.CreatedAt))
            .ToListAsync();

    public async Task<UserResponse> GetByIdAsync(int id) =>
        await _db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponse(u.Id, u.Email, u.DisplayName, u.CreatedAt))
            .FirstOrDefaultAsync()
        ?? throw new NotFoundException($"User {id} was not found.");

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new ConflictException($"A user with email '{email}' already exists.");

        var user = new User { Email = email, DisplayName = request.DisplayName.Trim() };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new UserResponse(user.Id, user.Email, user.DisplayName, user.CreatedAt);
    }
}