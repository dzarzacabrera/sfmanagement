using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;
using SFManagement.Web.ViewModels;

namespace SFManagement.Web.Controllers;

public class ProjectController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromServices] IGetAllProjectsQueryHandler handler,
        [FromServices] IIdEncryptionService enc,
        [FromServices] INpgsqlConnectionFactory connFactory)
    {
        var projects = await handler.HandleAsync(new GetAllProjectsQuery());

        var availableWorkersPerProject = new Dictionary<long, bool>();
        await using (var conn = await connFactory.GetOpenConnectionAsync())
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT p.id, (SELECT COUNT(1) FROM workers WHERE id NOT IN (SELECT worker_id FROM project_workers WHERE project_id = p.id)) AS available FROM projects p";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var pid = reader.GetInt64(0);
                var available = reader.GetInt64(1) > 0;
                availableWorkersPerProject[pid] = available;
            }
        }

        ViewBag.AvailableWorkersPerProject = availableWorkersPerProject;
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

        if (project.IsFinalized)
        {
            TempData["ToastError"] = "Cannot edit a closed project.";
            return RedirectToAction("Index");
        }

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
        [FromServices] IGetAllProjectsQueryHandler projectsHandler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(projectIdEncrypted, out var pid)) return NotFound();

        var projects = await projectsHandler.HandleAsync(new GetAllProjectsQuery());
        if (projects.FirstOrDefault(p => p.Id == pid) is { IsFinalized: true })
        {
            TempData["ToastError"] = "Cannot edit a closed project.";
            return RedirectToAction("Index");
        }

        var command = new UpdateProjectCommand(pid, name, descriptionMd);
        await handler.HandleAsync(command);
        return RedirectToAction("Detail", new { projectId = projectIdEncrypted });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(
        [FromQuery] string projectId,
        [FromServices] IGetAllProjectsQueryHandler projectHandler,
        [FromServices] IGetWorkersByProjectQueryHandler workersHandler,
        [FromServices] IGetDashboardTasksQueryHandler tasksHandler,
        [FromServices] IIdEncryptionService enc,
        [FromServices] INpgsqlConnectionFactory connFactory)
    {
        if (!enc.TryDecrypt(projectId, out var pid)) return NotFound();
        var projects = await projectHandler.HandleAsync(new GetAllProjectsQuery());
        var project = projects.FirstOrDefault(p => p.Id == pid);

        var workersTask = workersHandler.HandleAsync(new GetWorkersByProjectQuery(pid));
        var tasksTask = tasksHandler.HandleAsync(new GetDashboardTasksQuery(pid));

        var hasWorkersToAssignToProject = true;
        await using (var conn = await connFactory.GetOpenConnectionAsync())
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(1) FROM workers WHERE id NOT IN (SELECT worker_id FROM project_workers WHERE project_id = $1)";
            cmd.Parameters.Add(new() { Value = pid });
            var available = (long)(await cmd.ExecuteScalarAsync() ?? 0);
            hasWorkersToAssignToProject = available > 0;
        }

        var workers = await workersTask;
        var tasks = await tasksTask;

        var projectName = project?.Name ?? $"Project #{pid}";
        ViewBag.PageTitle = projectName;
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Projects", "/Project/Index"), new("Detail", "") };

        var vm = new ProjectDetailViewModel(
            pid,
            project?.Name ?? $"Project #{pid}",
            project?.DescriptionMd,
            workers.Select(w => w with { IdEncrypted = enc.Encrypt(w.Id) }).ToList(),
            tasks.Select(t => t with { IdEncrypted = enc.Encrypt(t.Id), ProjectIdEncrypted = enc.Encrypt(t.ProjectId) }).ToList(),
            hasWorkersToAssignToProject,
            project?.IsFinalized ?? false)
        {
            IdEncrypted = projectId
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> AddWorkerPopup(
        [FromQuery] string projectId,
        [FromServices] IGetWorkersNotInProjectQueryHandler handler,
        [FromServices] IGetAllSkillsQueryHandler skillsHandler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(projectId, out var pid)) return NotFound();
        var workers = await handler.HandleAsync(new GetWorkersNotInProjectQuery(pid));
        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());
        var vm = new AddWorkerToProjectPopupViewModel(pid,
            workers.Select(w => w with { IdEncrypted = enc.Encrypt(w.Id) }).ToList(),
            skills)
        {
            ProjectIdEncrypted = projectId
        };
        return PartialView("~/Views/Dashboard/_AddWorkerToProjectModal.cshtml", vm);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AddWorkersToProject(
        [FromForm] string projectIdEncrypted,
        [FromForm] string[] workerIdEncrypted,
        [FromServices] ICommandHandler<AddWorkersToProjectCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(projectIdEncrypted, out var pid)) return BadRequest();
        var ids = new List<long>();
        foreach (var w in workerIdEncrypted)
        {
            if (enc.TryDecrypt(w, out var wid))
                ids.Add(wid);
        }
        if (ids.Count == 0) return BadRequest();
        await handler.HandleAsync(new AddWorkersToProjectCommand(pid, ids));
        return Ok();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> FinalizeProject(
        [FromForm] string projectIdEncrypted,
        [FromServices] ICommandHandler<FinalizeProjectCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(projectIdEncrypted, out var pid)) return BadRequest();
        try
        {
            await handler.HandleAsync(new FinalizeProjectCommand(pid));
            return Json(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
