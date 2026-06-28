using Microsoft.AspNetCore.Mvc;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.Queries;
using SFManagement.Web.ViewModels;

namespace SFManagement.Web.Controllers;

public class ProjectController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromServices] IGetAllProjectsQueryHandler handler)
    {
        var projects = await handler.HandleAsync(new GetAllProjectsQuery());
        return View(projects);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateProjectViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromForm] string name,
        [FromForm] IFormFile? descriptionFile,
        [FromServices] ICommandHandler<CreateProjectCommand> handler)
    {
        string? descriptionMd = null;

        if (descriptionFile is not null && descriptionFile.Length > 0)
        {
            using var reader = new StreamReader(descriptionFile.OpenReadStream());
            descriptionMd = await reader.ReadToEndAsync();
        }

        var command = new CreateProjectCommand(name, descriptionMd);
        await handler.HandleAsync(command);
        return RedirectToAction("Index", "Dashboard", new { projectId = command.CreatedId });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(
        [FromQuery] int projectId)
    {
        return View(new ProjectDetailViewModel(projectId, $"Project #{projectId}", null));
    }
}
