using CloudTask.Api.Common;
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
    private readonly ICurrentUser _currentUser;

    public ProjectService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ProjectResponse>> GetAllAsync(ProjectQuery query)
    {
        IQueryable<Project> q = _db.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(p => p.Name.Contains(search));
        }

        var sortBy = query.SortBy.ToLowerInvariant();

        if (sortBy == "id")
        {
            q = query.Descending ? q.OrderByDescending(p => p.Id) : q.OrderBy(p => p.Id);
        }
        else if (sortBy == "name")
        {
            q = query.Descending
                ? q.OrderByDescending(p => p.Name).ThenBy(p => p.Id)
                : q.OrderBy(p => p.Name).ThenBy(p => p.Id);
        }
        else if (sortBy == "createdat")
        {
            q = query.Descending
                ? q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
                : q.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id);
        }
        else
        {
            throw new BadRequestException(
                $"Unknown sortBy '{query.SortBy}'. Use id, name, or createdAt.");
        }

        var totalCount = await q.CountAsync();

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProjectResponse(
                p.Id, p.Name, p.Description, p.CreatedAt, p.OwnerId, p.Tasks.Count))
            .ToListAsync();

        return new PagedResult<ProjectResponse>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<ProjectResponse> GetByIdAsync(int id)
    {
        var project = await _db.Projects.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectResponse(
                p.Id, p.Name, p.Description, p.CreatedAt, p.OwnerId, p.Tasks.Count))
            .FirstOrDefaultAsync();

        if (project is null)
        {
            throw new NotFoundException($"Project {id} was not found.");
        }

        return project;
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request)
    {
        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            OwnerId = _currentUser.Id
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return new ProjectResponse(
            project.Id, project.Name, project.Description,
            project.CreatedAt, project.OwnerId, 0);
    }

    public async Task UpdateAsync(int id, UpdateProjectRequest request)
    {
        var project = await FindOwnedAsync(id);

        project.Name = request.Name.Trim();
        project.Description = request.Description;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var project = await FindOwnedAsync(id);

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
    }

    private async Task<Project> FindOwnedAsync(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is null)
        {
            throw new NotFoundException($"Project {id} was not found.");
        }

        if (project.OwnerId != _currentUser.Id)
        {
            throw new ForbiddenException("Only the project owner can do this.");
        }

        return project;
    }
}