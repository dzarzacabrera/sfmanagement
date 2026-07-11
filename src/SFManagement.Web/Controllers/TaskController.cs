using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Domain.Enums;
using SFManagement.Web.ViewModels;

namespace SFManagement.Web.Controllers;

public class TaskController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromServices] IGetAllTasksQueryHandler handler,
        [FromServices] IGetAllProjectsQueryHandler projectsHandler,
        [FromServices] IIdEncryptionService enc)
    {
        var tasks = await handler.HandleAsync(new GetAllTasksQuery());
        var projects = await projectsHandler.HandleAsync(new GetAllProjectsQuery());
        ViewBag.Projects = projects.Select(p => p with { IdEncrypted = enc.Encrypt(p.Id) }).ToList();
        ViewBag.DefaultProjectIdEncrypted = projects.Count > 0 ? enc.Encrypt(projects[0].Id) : enc.Encrypt(1);
        ViewBag.PageTitle = "Tasks";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Tasks", "") };
        return View(tasks.Select(t => t with { IdEncrypted = enc.Encrypt(t.Id), ProjectIdEncrypted = enc.Encrypt(t.ProjectId) }).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        [FromQuery] string? projectId,
        [FromServices] IGetAllProjectsQueryHandler projectsHandler,
        [FromServices] IGetAllSkillsQueryHandler skillsHandler,
        [FromServices] IIdEncryptionService enc)
    {
        var projects = await projectsHandler.HandleAsync(new GetAllProjectsQuery());
        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());
        var criticalities = new List<CriticalityOption>
        {
            new(Criticality.Low, "Low"),
            new(Criticality.Medium, "Medium"),
            new(Criticality.High, "High"),
            new(Criticality.Critical, "Critical"),
        };

        ViewBag.AllSkills = skills;
        ViewBag.PageTitle = "Create Task";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Tasks", "/Task/Index"), new("Create Task", "") };

        long defaultPid;
        if (projectId != null && enc.TryDecrypt(projectId, out var pid))
            defaultPid = pid;
        else if (projectId != null && long.TryParse(projectId, out var ppid))
            defaultPid = ppid;
        else
            defaultPid = projects.Count > 0 ? projects[0].Id : 1;

        return View(new CreateTaskViewModel(defaultPid, projects.Select(p => p with { IdEncrypted = enc.Encrypt(p.Id) }).ToList(), skills.Select(s => new SkillCatalogueItem(s.Id, s.Name, s.VectorPosition) { IdEncrypted = enc.Encrypt(s.Id) }).ToList(), criticalities)
        {
            ProjectIdEncrypted = enc.Encrypt(defaultPid)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(
        [FromQuery] string taskId,
        [FromServices] IGetTaskByIdQueryHandler handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(taskId, out var tid)) return NotFound();
        var task = await handler.HandleAsync(new GetTaskByIdQuery(tid));
        if (task is null) return NotFound();

        ViewBag.PageTitle = task.Title;
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Tasks", "/Task/Index"), new("Detail", "") };
        return View(task with { IdEncrypted = enc.Encrypt(tid) });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        [FromQuery] string taskId,
        [FromServices] IGetTaskByIdQueryHandler taskHandler,
        [FromServices] IGetAllProjectsQueryHandler projectsHandler,
        [FromServices] IGetAllSkillsQueryHandler skillsHandler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(taskId, out var tid)) return NotFound();
        var task = await taskHandler.HandleAsync(new GetTaskByIdQuery(tid));
        if (task is null) return NotFound();

        if (task.Status is not (ProjectTaskStatus.Queued or ProjectTaskStatus.InProgress))
        {
            TempData["ToastError"] = "Only queued or in-progress tasks can be edited.";
            return RedirectToAction("Index");
        }

        var projects = await projectsHandler.HandleAsync(new GetAllProjectsQuery());
        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());

        var criticalities = new List<CriticalityOption>
        {
            new(Criticality.Low, "Low"),
            new(Criticality.Medium, "Medium"),
            new(Criticality.High, "High"),
            new(Criticality.Critical, "Critical"),
        };

        ViewBag.AllSkills = skills;
        ViewBag.PageTitle = $"Edit: {task.Title}";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>>
        {
            new("Tasks", "/Task/Index"),
            new("Edit", ""),
        };

        var skillsJson = task.Skills?.Select(s => new { pos = s.SkillPosition, level = (double)s.RequiredLevel });
        ViewBag.InitialSkillsJson = System.Text.Json.JsonSerializer.Serialize(skillsJson ?? Enumerable.Empty<object>());

        var encryptedPid = enc.Encrypt(task.ProjectId);
        return View(new EditTaskViewModel(
            tid, task.ProjectId, task.Title, task.Description, task.Criticality,
            projects.Select(p => p with { IdEncrypted = enc.Encrypt(p.Id) }).ToList(),
            skills.Select(s => new SkillCatalogueItem(s.Id, s.Name, s.VectorPosition) { IdEncrypted = enc.Encrypt(s.Id) }).ToList(),
            criticalities)
        {
            TaskIdEncrypted = taskId,
            ProjectIdEncrypted = encryptedPid
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        [FromForm] string taskIdEncrypted,
        [FromForm] string projectIdEncrypted,
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] string criticality,
        [FromForm] int[] skillPositions,
        [FromForm] string[] skillLevels,
        [FromServices] ICommandHandler<UpdateTaskCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(taskIdEncrypted, out var tid)) return NotFound();
        if (!enc.TryDecrypt(projectIdEncrypted, out var pid)) return NotFound();

        skillPositions = skillPositions ?? [];

        var parsed = new float[skillLevels?.Length ?? 0];
        for (int i = 0; i < (skillLevels?.Length ?? 0); i++)
            float.TryParse(skillLevels![i], NumberStyles.Float, CultureInfo.InvariantCulture, out parsed[i]);

        var vector = new float[1024];
        for (int i = 0; i < skillPositions.Length && i < parsed.Length; i++)
        {
            var pos = skillPositions[i];
            if (pos >= 0 && pos < 1024)
                vector[pos] = Math.Clamp(parsed[i], 0.0f, 10.0f);
        }

        var command = new UpdateTaskCommand(
            tid, pid, title, description,
            Enum.Parse<Criticality>(criticality, ignoreCase: true),
            vector);

        try
        {
            await handler.HandleAsync(command);
            TempData["ToastSuccess"] = "Task updated successfully.";
            return RedirectToAction("Detail", new { taskId = taskIdEncrypted });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ToastError"] = ex.Message;
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromForm] string projectIdEncrypted,
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] string criticality,
        [FromForm] int[] skillPositions,
        [FromForm] string[] skillLevels,
        [FromServices] ICommandHandler<CreateTaskCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(projectIdEncrypted, out var pid)) return NotFound();
        skillPositions = skillPositions ?? [];
        var parsed = new float[skillLevels?.Length ?? 0];
        for (int i = 0; i < (skillLevels?.Length ?? 0); i++)
            float.TryParse(skillLevels![i], NumberStyles.Float, CultureInfo.InvariantCulture, out parsed[i]);
        var vector = new float[1024];
        for (int i = 0; i < skillPositions.Length && i < parsed.Length; i++)
        {
            var pos = skillPositions[i];
            if (pos >= 0 && pos < 1024)
                vector[pos] = Math.Clamp(parsed[i], 0.0f, 10.0f);
        }

        var command = new CreateTaskCommand(
            pid, title, description,
            Enum.Parse<Criticality>(criticality, ignoreCase: true),
            vector);
        await handler.HandleAsync(command);
        return RedirectToAction("Index", "Dashboard", new { projectId = projectIdEncrypted });
    }
}
