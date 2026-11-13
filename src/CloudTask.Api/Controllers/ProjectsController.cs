using CloudTask.Api.Dtos;
using CloudTask.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudTask.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projects;

    public ProjectsController(IProjectService projects) => _projects = projects;

    [HttpGet]
    public async Task<ActionResult<List<ProjectResponse>>> GetAll() =>
        Ok(await _projects.GetAllAsync());

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectResponse>>> GetAll(
    [FromQuery] ProjectQuery query) =>
    Ok(await _projects.GetAllAsync(query));
    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request)
    {
        var created = await _projects.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProjectRequest request)
    {
        await _projects.UpdateAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _projects.DeleteAsync(id);
        return NoContent();
    }
}