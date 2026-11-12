using CloudTask.Api.Dtos;
using CloudTask.Domain.Entities;
using CloudTask.Domain.Enums;
using CloudTask.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudTask.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/tasks")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TaskResponse>>> GetAll(
        int projectId, [FromQuery] TaskItemStatus? status)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId))
            return NotFound();

        var query = _db.Tasks.AsNoTracking().Where(t => t.ProjectId == projectId);
        if (status is not null)
            query = query.Where(t => t.Status == status);

        var tasks = await query.OrderBy(t => t.Id).ToListAsync();
        return Ok(tasks.Select(TaskResponse.From).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskResponse>> GetById(int projectId, int id)
    {
        var task = await FindTaskAsync(projectId, id);
        return task is null ? NotFound() : Ok(TaskResponse.From(task));
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(int projectId, CreateTaskRequest request)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project is null) return NotFound();

        var task = project.AddTask(request.Title, request.Priority);
        task.Description = request.Description;
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { projectId, id = task.Id }, TaskResponse.From(task));
    }

    [HttpPost("{id:int}/start")]
    public Task<IActionResult> Start(int projectId, int id) =>
        ApplyAsync(projectId, id, task => task.Start());

    [HttpPost("{id:int}/complete")]
    public Task<IActionResult> Complete(int projectId, int id) =>
        ApplyAsync(projectId, id, task => task.Complete());

    [HttpPost("{id:int}/assign")]
    public async Task<IActionResult> Assign(int projectId, int id, AssignTaskRequest request)
    {
        var user = await _db.Users.FindAsync(request.UserId);
        if (user is null)
            return BadRequest(new { error = $"User {request.UserId} does not exist." });

        return await ApplyAsync(projectId, id, task => task.AssignTo(user));
    }

    private Task<TaskItem?> FindTaskAsync(int projectId, int id) =>
        _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId);

    // Loads the task, runs a domain operation, saves, and maps rule violations to 409
    private async Task<IActionResult> ApplyAsync(int projectId, int id, Action<TaskItem> operation)
    {
        var task = await FindTaskAsync(projectId, id);
        if (task is null) return NotFound();

        try
        {
            operation(task);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }

        await _db.SaveChangesAsync();
        return Ok(TaskResponse.From(task));
    }
}