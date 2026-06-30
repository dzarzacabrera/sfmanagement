using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.DTOs;
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
        ViewBag.PageTitle = "Projects";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Projects", "") };
        return View(projects);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.PageTitle = "Create Project";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Projects", "/Project/Index"), new("Create", "") };
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
        [FromQuery] int projectId,
        [FromServices] IGetAllProjectsQueryHandler projectHandler,
        [FromServices] IGetWorkersByProjectQueryHandler workersHandler)
    {
        var projects = await projectHandler.HandleAsync(new GetAllProjectsQuery());
        var project = projects.FirstOrDefault(p => p.Id == projectId);

        var workers = await workersHandler.HandleAsync(new GetWorkersByProjectQuery(projectId));

        ViewBag.PageTitle = $"Project #{projectId}";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Projects", "/Project/Index"), new($"Project #{projectId}", "") };

        var vm = new ProjectDetailViewModel(
            projectId,
            project?.Name ?? $"Project #{projectId}",
            project?.DescriptionMd,
            workers);

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> AddWorkerPopup(
        [FromQuery] int projectId,
        [FromServices] IGetWorkersNotInProjectQueryHandler handler)
    {
        var workers = await handler.HandleAsync(new GetWorkersNotInProjectQuery(projectId));
        var vm = new AddWorkerToProjectPopupViewModel(projectId, workers);
        return PartialView("~/Views/Dashboard/_AddWorkerToProjectModal.cshtml", vm);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AddWorkerToProject(
        [FromForm] int projectId,
        [FromForm] int workerId,
        [FromServices] ICommandHandler<AddWorkerToProjectCommand> handler)
    {
        await handler.HandleAsync(new AddWorkerToProjectCommand(projectId, workerId));
        return Ok();
    }
}
