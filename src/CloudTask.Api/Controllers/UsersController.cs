using CloudTask.Api.Dtos;
using CloudTask.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudTask.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll() =>
        Ok(await _users.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id) =>
        Ok(await _users.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
    {
        var created = await _users.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}