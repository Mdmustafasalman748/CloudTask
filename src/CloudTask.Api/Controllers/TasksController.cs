using CloudTask.Api.Common;
using CloudTask.Api.Dtos;
using CloudTask.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudTask.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/projects/{projectId:int}/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _tasks;

    public TasksController(ITaskService tasks)
    {
        _tasks = tasks;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskResponse>>> GetAll(
        int projectId, [FromQuery] TaskQuery query)
    {
        return Ok(await _tasks.GetAllAsync(projectId, query));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskResponse>> GetById(int projectId, int id)
    {
        return Ok(await _tasks.GetByIdAsync(projectId, id));
    }

    /// <summary>Creates a task. Project owner only.</summary>
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(int projectId, CreateTaskRequest request)
    {
        var created = await _tasks.CreateAsync(projectId, request);
        return CreatedAtAction(nameof(GetById), new { projectId, id = created.Id }, created);
    }

    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<TaskResponse>> Start(int projectId, int id)
    {
        return Ok(await _tasks.StartAsync(projectId, id));
    }

    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<TaskResponse>> Complete(int projectId, int id)
    {
        return Ok(await _tasks.CompleteAsync(projectId, id));
    }

    [HttpPost("{id:int}/assign")]
    public async Task<ActionResult<TaskResponse>> Assign(
        int projectId, int id, AssignTaskRequest request)
    {
        return Ok(await _tasks.AssignAsync(projectId, id, request.UserId));
    }
}