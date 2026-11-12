using CloudTask.Api.Dtos;
using CloudTask.Api.Services;
using CloudTask.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudTask.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _tasks;

    public TasksController(ITaskService tasks) => _tasks = tasks;

    [HttpGet]
    public async Task<ActionResult<List<TaskResponse>>> GetAll(
        int projectId, [FromQuery] TaskItemStatus? status) =>
        Ok(await _tasks.GetAllAsync(projectId, status));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskResponse>> GetById(int projectId, int id) =>
        Ok(await _tasks.GetByIdAsync(projectId, id));

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(int projectId, CreateTaskRequest request)
    {
        var created = await _tasks.CreateAsync(projectId, request);
        return CreatedAtAction(nameof(GetById), new { projectId, id = created.Id }, created);
    }

    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<TaskResponse>> Start(int projectId, int id) =>
        Ok(await _tasks.StartAsync(projectId, id));

    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<TaskResponse>> Complete(int projectId, int id) =>
        Ok(await _tasks.CompleteAsync(projectId, id));

    [HttpPost("{id:int}/assign")]
    public async Task<ActionResult<TaskResponse>> Assign(
        int projectId, int id, AssignTaskRequest request) =>
        Ok(await _tasks.AssignAsync(projectId, id, request.UserId));
}