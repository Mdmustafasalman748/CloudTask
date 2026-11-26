using System.Diagnostics;
using CloudTask.Web.Models;
using CloudTask.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudTask.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api)
    {
        _api = api;
    }

    [Authorize]
    public async Task<IActionResult> Index(int page = 1)
    {
        var token = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(token))
        {
            return await SignOutAndRedirectAsync();
        }

        var projects = await _api.GetProjectsAsync(token, page < 1 ? 1 : page);

        // The API rejected the token (expired), so make the user log in again
        if (projects.StatusCode == 401)
        {
            return await SignOutAndRedirectAsync();
        }

        if (!projects.Success)
        {
            TempData["Error"] = projects.Error;
            return View(new PagedDto<ProjectDto>(new List<ProjectDto>(), 1, 10, 0, 0));
        }

        return View(projects.Data!);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    private async Task<IActionResult> SignOutAndRedirectAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }
}