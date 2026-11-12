using CloudTask.Api.Common;
using CloudTask.Api.Dtos;
using CloudTask.Domain.Entities;
using CloudTask.Domain.Enums;
using CloudTask.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace CloudTask.Api.Services;

public interface ITaskService
{
    Task<List<TaskResponse>> GetAllAsync(int projectId, TaskItemStatus? status);
    Task<TaskResponse> GetByIdAsync(int projectId, int id);
    Task<TaskResponse> CreateAsync(int projectId, CreateTaskRequest request);
    Task<TaskResponse> StartAsync(int projectId, int id);
    Task<TaskResponse> CompleteAsync(int projectId, int id);
    Task<TaskResponse> AssignAsync(int projectId, int id, int userId);
}

public class TaskService : ITaskService
{
    private readonly AppDbContext _db;

    public TaskService(AppDbContext db) => _db = db;

    public async Task<List<TaskResponse>> GetAllAsync(int projectId, TaskItemStatus? status)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId))
            throw new NotFoundException($"Project {projectId} was not found.");

        var query = _db.Tasks.AsNoTracking().Where(t => t.ProjectId == projectId);
        if (status is not null)
            query = query.Where(t => t.Status == status);

        var tasks = await query.OrderBy(t => t.Id).ToListAsync();
        return tasks.Select(TaskResponse.From).ToList();
    }

    public async Task<TaskResponse> GetByIdAsync(int projectId, int id) =>
        TaskResponse.From(await FindAsync(projectId, id));

    public async Task<TaskResponse> CreateAsync(int projectId, CreateTaskRequest request)
    {
        var project = await _db.Projects.FindAsync(projectId)
            ?? throw new NotFoundException($"Project {projectId} was not found.");

        var task = project.AddTask(request.Title, request.Priority);
        task.Description = request.Description;
        await _db.SaveChangesAsync();

        return TaskResponse.From(task);
    }

    public Task<TaskResponse> StartAsync(int projectId, int id) =>
        ApplyAsync(projectId, id, task => task.Start());

    public Task<TaskResponse> CompleteAsync(int projectId, int id) =>
        ApplyAsync(projectId, id, task => task.Complete());

    public async Task<TaskResponse> AssignAsync(int projectId, int id, int userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new BadRequestException($"User {userId} does not exist.");

        return await ApplyAsync(projectId, id, task => task.AssignTo(user));
    }

    private async Task<TaskItem> FindAsync(int projectId, int id) =>
        await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId)
        ?? throw new NotFoundException($"Task {id} was not found in project {projectId}.");

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