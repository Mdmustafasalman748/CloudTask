using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CloudTask.Web.Models;
using CloudTask.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudTask.Web.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly ApiClient _api;

    public ProjectsController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateProjectViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProjectViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var token = await GetTokenAsync();
        var result = await _api.CreateProjectAsync(token, model.Name, model.Description);

        if (result.StatusCode == 401)
        {
            return await ExpiredAsync();
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not create the project.");
            return View(model);
        }

        TempData["Success"] = "Project created.";
        return RedirectToAction("Details", new { id = result.Data!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var token = await GetTokenAsync();

        var project = await _api.GetProjectAsync(token, id);
        if (project.StatusCode == 401)
        {
            return await ExpiredAsync();
        }

        if (project.StatusCode == 404)
        {
            return NotFound();
        }

        if (!project.Success)
        {
            TempData["Error"] = project.Error;
            return RedirectToAction("Index", "Home");
        }

        var tasks = await _api.GetTasksAsync(token, id);

        var vm = new ProjectDetailsViewModel
        {
            Project = project.Data!,
            Tasks = tasks.Success ? tasks.Data!.Items : new List<TaskDto>(),
            // Only the owner may see tasks; the API answers 403 to everyone else
            TasksVisible = tasks.Success,
            CurrentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTask(int id, string title, string priority)
    {
        var token = await GetTokenAsync();
        var result = await _api.CreateTaskAsync(token, id, title, priority);

        if (result.StatusCode == 401)
        {
            return await ExpiredAsync();
        }

        if (result.Success)
        {
            TempData["Success"] = "Task added.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaskAction(int id, int taskId, string action)
    {
        // Whitelist the action so the URL can't be steered anywhere else
        if (action != "start" && action != "complete")
        {
            return BadRequest();
        }

        var token = await GetTokenAsync();
        var result = await _api.TaskActionAsync(token, id, taskId, action);

        if (result.StatusCode == 401)
        {
            return await ExpiredAsync();
        }

        if (result.Success)
        {
            TempData["Success"] = action == "start" ? "Task started." : "Task completed.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction("Details", new { id });
    }

    private async Task<string> GetTokenAsync()
    {
        return await HttpContext.GetTokenAsync("access_token") ?? string.Empty;
    }

    private async Task<IActionResult> ExpiredAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }
}

public class CreateProjectViewModel
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }
}

public class ProjectDetailsViewModel
{
    public ProjectDetailDto Project { get; set; } = null!;
    public List<TaskDto> Tasks { get; set; } = new();
    public bool TasksVisible { get; set; }
    public int CurrentUserId { get; set; }
}