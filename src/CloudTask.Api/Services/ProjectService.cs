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
}using CloudTask.Api.Common;
using CloudTask.Api.Dtos;
using CloudTask.Domain.Entities;
using CloudTask.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloudTask.Api.Services;

public interface IProjectService
{
    Task<PagedResult<ProjectResponse>> GetAllAsync(ProjectQuery query);
    Task<ProjectResponse> GetByIdAsync(int id);
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request);
    Task UpdateAsync(int id, UpdateProjectRequest request);
    Task DeleteAsync(int id);
}

public class ProjectService : IProjectService
{
    private readonly AppDbContext _db;

    public ProjectService(AppDbContext db) => _db = db;

    public async Task<PagedResult<ProjectResponse>> GetAllAsync(ProjectQuery query)
    {
        IQueryable<Project> q = _db.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(p => p.Name.Contains(search));
        }

        // Whitelisted sorting; Id is the tiebreaker so paging stays stable
        q = (query.SortBy.ToLowerInvariant(), query.Descending) switch
        {
            ("id", false) => q.OrderBy(p => p.Id),
            ("id", true) => q.OrderByDescending(p => p.Id),
            ("name", false) => q.OrderBy(p => p.Name).ThenBy(p => p.Id),
            ("name", true) => q.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
            ("createdat", false) => q.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            ("createdat", true) => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            _ => throw new BadRequestException(
                $"Unknown sortBy '{query.SortBy}'. Use id, name, or createdAt.")
        };

        var totalCount = await q.CountAsync();

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProjectResponse(
                p.Id, p.Name, p.Description, p.CreatedAt, p.OwnerId, p.Tasks.Count))
            .ToListAsync();

        return new PagedResult<ProjectResponse>(items, query.Page, query.PageSize, totalCount);
    }

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