using CloudTask.Api.Common;
using CloudTask.Api.Dtos;
using CloudTask.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudTask.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projects;

    public ProjectsController(IProjectService projects)
    {
        _projects = projects;
    }

    /// <summary>Lists projects with paging, sorting, and name search.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<ProjectResponse>>> GetAll(
        [FromQuery] ProjectQuery query)
    {
        return Ok(await _projects.GetAllAsync(query));
    }

    /// <summary>Gets one project by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProjectResponse>> GetById(int id)
    {
        return Ok(await _projects.GetByIdAsync(id));
    }

    /// <summary>Creates a project owned by the logged-in user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request)
    {
        var created = await _projects.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates a project. Owner only.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateProjectRequest request)
    {
        await _projects.UpdateAsync(id, request);
        return NoContent();
    }

    /// <summary>Deletes a project and all of its tasks. Owner only.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _projects.DeleteAsync(id);
        return NoContent();
    }
}