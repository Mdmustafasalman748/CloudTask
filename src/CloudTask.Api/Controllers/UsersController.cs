using CloudTask.Api.Common;
using CloudTask.Api.Dtos;
using CloudTask.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudTask.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ITaskService _tasks;

    public UsersController(IUserService users, ITaskService tasks)
    {
        _users = users;
        _tasks = tasks;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll()
    {
        return Ok(await _users.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id)
    {
        return Ok(await _users.GetByIdAsync(id));
    }

    [HttpGet("{id:int}/tasks")]
    public async Task<ActionResult<PagedResult<AssignedTaskResponse>>> GetAssignedTasks(
        int id, [FromQuery] TaskQuery query)
    {
        return Ok(await _tasks.GetAssignedAsync(id, query));
    }
}