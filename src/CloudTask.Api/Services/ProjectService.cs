using CloudTask.Api.Common;
using CloudTask.Api.Dtos;
using CloudTask.Domain.Entities;
using CloudTask.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudTask.Api.Services;

public interface IProjectService
{
    Task<List<ProjectResponse>> GetAllAsync();
    Task<ProjectResponse> GetByIdAsync(int id);
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request);
    Task UpdateAsync(int id, UpdateProjectRequest request);
    Task DeleteAsync(int id);
}

public class ProjectService : IProjectService
{
    private readonly AppDbContext _db;

    public ProjectService(AppDbContext db) => _db = db;

    public Task<List<ProjectResponse>> GetAllAsync() =>
        _db.Projects.AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new ProjectResponse(
                p.Id, p.Name, p.Description, p.CreatedAt, p.OwnerId, p.Tasks.Count))
            .ToListAsync();

    public async Task<ProjectResponse> GetByIdAsync(int id) =>
        await _db.Projects.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectResponse(
                p.Id, p.Name, p.Description, p.CreatedAt, p.OwnerId, p.Tasks.Count))
            .FirstOrDefaultAsync()
        ?? throw new NotFoundException($"Project {id} was not found.");

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == request.OwnerId))
            throw new BadRequestException($"User {request.OwnerId} does not exist.");

        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            OwnerId = request.OwnerId
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return new ProjectResponse(
            project.Id, project.Name, project.Description,
            project.CreatedAt, project.OwnerId, 0);
    }

    public async Task UpdateAsync(int id, UpdateProjectRequest request)
    {
        var project = await _db.Projects.FindAsync(id)
            ?? throw new NotFoundException($"Project {id} was not found.");

        project.Name = request.Name.Trim();
        project.Description = request.Description;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var project = await _db.Projects.FindAsync(id)
            ?? throw new NotFoundException($"Project {id} was not found.");

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
    }
}