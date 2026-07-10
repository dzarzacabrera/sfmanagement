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
        [FromServices] IGetAllProjectsQueryHandler handler,
        [FromServices] IIdEncryptionService enc)
    {
        var projects = await handler.HandleAsync(new GetAllProjectsQuery());
        ViewBag.PageTitle = "Projects";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Projects", "") };
        return View(projects.Select(p => p with { IdEncrypted = enc.Encrypt(p.Id) }).ToList());
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
        [FromServices] ICommandHandler<CreateProjectCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        string? descriptionMd = null;

        if (descriptionFile is not null && descriptionFile.Length > 0)
        {
            using var reader = new StreamReader(descriptionFile.OpenReadStream());
            descriptionMd = await reader.ReadToEndAsync();
        }

        var command = new CreateProjectCommand(name, descriptionMd);
        await handler.HandleAsync(command);
        return RedirectToAction("Index", "Dashboard", new { projectId = enc.Encrypt(command.CreatedId) });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        [FromQuery] string projectId,
        [FromServices] IGetAllProjectsQueryHandler handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(projectId, out var pid)) return NotFound();
        var projects = await handler.HandleAsync(new GetAllProjectsQuery());
        var project = projects.FirstOrDefault(p => p.Id == pid);
        if (project is null) return NotFound();

        ViewBag.PageTitle = "Edit Project";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Projects", "/Project/Index"), new("Edit", "") };
        return View(project with { IdEncrypted = enc.Encrypt(pid) });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        [FromForm] string projectIdEncrypted,
        [FromForm] string name,
        [FromForm] string? descriptionMd,
        [FromServices] ICommandHandler<UpdateProjectCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(projectIdEncrypted, out var pid)) return NotFound();
        var command = new UpdateProjectCommand(pid, name, descriptionMd);
        await handler.HandleAsync(command);
        return RedirectToAction("Detail", new { projectId = projectIdEncrypted });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(
        [FromQuery] string projectId,
        [FromServices] IGetAllProjectsQueryHandler projectHandler,
        [FromServices] IGetWorkersByProjectQueryHandler workersHandler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(projectId, out var pid)) return NotFound();
        var projects = await projectHandler.HandleAsync(new GetAllProjectsQuery());
        var project = projects.FirstOrDefault(p => p.Id == pid);

        var workers = await workersHandler.HandleAsync(new GetWorkersByProjectQuery(pid));

        ViewBag.PageTitle = $"Project #{pid}";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Projects", "/Project/Index"), new($"Project #{pid}", "") };

        var vm = new ProjectDetailViewModel(
            pid,
            project?.Name ?? $"Project #{pid}",
            project?.DescriptionMd,
            workers.Select(w => w with { IdEncrypted = enc.Encrypt(w.Id) }).ToList())
        {
            IdEncrypted = projectId
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> AddWorkerPopup(
        [FromQuery] string projectId,
        [FromServices] IGetWorkersNotInProjectQueryHandler handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(projectId, out var pid)) return NotFound();
        var workers = await handler.HandleAsync(new GetWorkersNotInProjectQuery(pid));
        var vm = new AddWorkerToProjectPopupViewModel(pid,
            workers.Select(w => w with { IdEncrypted = enc.Encrypt(w.Id) }).ToList())
        {
            ProjectIdEncrypted = projectId
        };
        return PartialView("~/Views/Dashboard/_AddWorkerToProjectModal.cshtml", vm);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AddWorkerToProject(
        [FromForm] string projectIdEncrypted,
        [FromForm] string workerIdEncrypted,
        [FromServices] ICommandHandler<AddWorkerToProjectCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(projectIdEncrypted, out var pid)) return BadRequest();
        if (!enc.TryDecrypt(workerIdEncrypted, out var wid)) return BadRequest();
        await handler.HandleAsync(new AddWorkerToProjectCommand(pid, wid));
        return Ok();
    }
}
