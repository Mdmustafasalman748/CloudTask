using CloudTask.Api.Common;
using CloudTask.Api.Dtos;
using CloudTask.Domain.Entities;
using CloudTask.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace CloudTask.Api.Services;

public interface ITaskService
{
    Task<PagedResult<TaskResponse>> GetAllAsync(int projectId, TaskQuery query);
    Task<PagedResult<AssignedTaskResponse>> GetAssignedAsync(int userId, TaskQuery query);
    Task<TaskResponse> GetByIdAsync(int projectId, int id);
    Task<TaskResponse> CreateAsync(int projectId, CreateTaskRequest request);
    Task<TaskResponse> StartAsync(int projectId, int id);
    Task<TaskResponse> CompleteAsync(int projectId, int id);
    Task<TaskResponse> AssignAsync(int projectId, int id, int userId);
}

public class TaskService : ITaskService
{
    private readonly AppDbContext _db;

    public TaskService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<TaskResponse>> GetAllAsync(int projectId, TaskQuery query)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId))
        {
            throw new NotFoundException($"Project {projectId} was not found.");
        }

        var q = FilterAndSort(
            _db.Tasks.AsNoTracking().Where(t => t.ProjectId == projectId), query);

        var totalCount = await q.CountAsync();
        var tasks = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<TaskResponse>(
            tasks.Select(TaskResponse.From).ToList(), query.Page, query.PageSize, totalCount);
    }

    // Tasks assigned to one user across ALL projects, with the project name in ONE query
    public async Task<PagedResult<AssignedTaskResponse>> GetAssignedAsync(int userId, TaskQuery query)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId))
        {
            throw new NotFoundException($"User {userId} was not found.");
        }

        var q = FilterAndSort(
            _db.Tasks.AsNoTracking().Where(t => t.AssigneeId == userId), query);

        var totalCount = await q.CountAsync();
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new AssignedTaskResponse(
                t.Id, t.Title, t.Status.ToString(), t.Priority.ToString(),
                t.ProjectId, t.Project!.Name, t.CreatedAt))
            .ToListAsync();

        return new PagedResult<AssignedTaskResponse>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<TaskResponse> GetByIdAsync(int projectId, int id)
    {
        var task = await FindAsync(projectId, id);
        return TaskResponse.From(task);
    }

    public async Task<TaskResponse> CreateAsync(int projectId, CreateTaskRequest request)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project is null)
        {
            throw new NotFoundException($"Project {projectId} was not found.");
        }

        var task = project.AddTask(request.Title, request.Priority);
        task.Description = request.Description;
        await _db.SaveChangesAsync();

        return TaskResponse.From(task);
    }

    public Task<TaskResponse> StartAsync(int projectId, int id)
    {
        return ApplyAsync(projectId, id, task => task.Start());
    }

    public Task<TaskResponse> CompleteAsync(int projectId, int id)
    {
        return ApplyAsync(projectId, id, task => task.Complete());
    }

    public async Task<TaskResponse> AssignAsync(int projectId, int id, int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            throw new BadRequestException($"User {userId} does not exist.");
        }

        return await ApplyAsync(projectId, id, task => task.AssignTo(user));
    }

    // Shared by both list endpoints: status filter + whitelisted sort (Id is the tiebreaker)
    private static IQueryable<TaskItem> FilterAndSort(IQueryable<TaskItem> q, TaskQuery query)
    {
        if (query.Status is { } status)
        {
            q = q.Where(t => t.Status == status);
        }

        var sortBy = query.SortBy.ToLowerInvariant();

        if (sortBy == "id")
        {
            return query.Descending ? q.OrderByDescending(t => t.Id) : q.OrderBy(t => t.Id);
        }

        if (sortBy == "title")
        {
            return query.Descending
                ? q.OrderByDescending(t => t.Title).ThenBy(t => t.Id)
                : q.OrderBy(t => t.Title).ThenBy(t => t.Id);
        }

        if (sortBy == "createdat")
        {
            return query.Descending
                ? q.OrderByDescending(t => t.CreatedAt).ThenBy(t => t.Id)
                : q.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id);
        }

        throw new BadRequestException(
            $"Unknown sortBy '{query.SortBy}'. Use id, title, or createdAt.");
    }

    private async Task<TaskItem> FindAsync(int projectId, int id)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId);
        if (task is null)
        {
            throw new NotFoundException($"Task {id} was not found in project {projectId}.");
        }

        return task;
    }

    // Load, run the domain operation, translate a rule violation into a conflict, save
    private async Task<TaskResponse> ApplyAsync(int projectId, int id, Action<TaskItem> operation)
    {
        var task = await FindAsync(projectId, id);

        try
        {
            operation(task);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync();
        return TaskResponse.From(task);
    }
}