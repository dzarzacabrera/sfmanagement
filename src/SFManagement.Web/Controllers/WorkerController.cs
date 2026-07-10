using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.Queries;
using SFManagement.Web.ViewModels;
using SFManagement.Application.DTOs;

namespace SFManagement.Web.Controllers;

public class WorkerController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(
        [FromServices] IGetAllSkillsQueryHandler skillsHandler,
        [FromServices] IIdEncryptionService enc)
    {
        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());
        ViewBag.AllSkills = skills;
        ViewBag.PageTitle = "Add Worker";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Workers", "/Worker/Index"), new("Add Worker", "") };
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromForm] string name,
        [FromForm] string? role,
        [FromForm] int[]? skillPositions,
        [FromForm] float[]? skillLevels,
        [FromServices] ICommandHandler<CreateWorkerCommand> handler)
    {
        var vector = new float[1024];
        if (skillPositions is not null && skillLevels is not null)
        {
            int count = Math.Min(skillPositions.Length, skillLevels.Length);
            for (int i = 0; i < count; i++)
            {
                var pos = skillPositions[i];
                if (pos >= 0 && pos < 1024)
                    vector[pos] = Math.Clamp(skillLevels[i], 0.0f, 10.0f);
            }
        }

        var command = new CreateWorkerCommand(name, role ?? string.Empty, vector);
        await handler.HandleAsync(command);
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromServices] IGetAllWorkersQueryHandler handler,
        [FromServices] IGetAllSkillsQueryHandler skillsHandler,
        [FromServices] IIdEncryptionService enc)
    {
        var workers = await handler.HandleAsync(new GetAllWorkersQuery());
        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());
        ViewBag.AllSkills = skills;
        ViewBag.PageTitle = "Workers";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Workers", "") };
        return View(workers.Select(w => w with { IdEncrypted = enc.Encrypt(w.Id) }).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Detail(
        [FromQuery] string workerId,
        [FromServices] IGetAllWorkersQueryHandler allWorkersHandler,
        [FromServices] IGetWorkerHistoryQueryHandler historyHandler,
        [FromServices] IGetWorkerTasksQueryHandler tasksHandler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(workerId, out var wid)) return NotFound();
        var workers = await allWorkersHandler.HandleAsync(new GetAllWorkersQuery());
        var worker = workers.FirstOrDefault(w => w.Id == wid);
        if (worker is null) return NotFound();

        var evaluations = await historyHandler.HandleAsync(new GetWorkerHistoryQuery(wid));
        var tasks = (await tasksHandler.HandleAsync(new GetWorkerTasksQuery(wid)))
            .Select(t => t with { TaskIdEncrypted = enc.Encrypt(t.TaskId), ProjectIdEncrypted = enc.Encrypt(t.ProjectId) })
            .ToList();

        var vm = new WorkerHistoryViewModel(
            wid,
            worker.Name,
            evaluations,
            tasks)
        {
            WorkerIdEncrypted = enc.Encrypt(wid)
        };

        ViewBag.PageTitle = worker.Name;
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Workers", "/Worker/Index"), new("Detail", "") };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        [FromQuery] string workerId,
        [FromServices] IGetAllWorkersQueryHandler allWorkersHandler,
        [FromServices] IGetAllSkillsQueryHandler skillsHandler,
        [FromServices] IGetWorkerHistoryQueryHandler historyHandler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(workerId, out var wid)) return NotFound();
        var workers = await allWorkersHandler.HandleAsync(new GetAllWorkersQuery());
        var worker = workers.FirstOrDefault(w => w.Id == wid);
        if (worker is null) return NotFound();

        var evaluations = await historyHandler.HandleAsync(new GetWorkerHistoryQuery(wid));
        var vm = new WorkerHistoryViewModel(wid, worker.Name, evaluations);

        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());
        ViewBag.AllSkills = skills;
        ViewBag.WorkerRole = worker.Role;

        var activeSkills = skills
            .Select(s => new { pos = s.VectorPosition, level = Math.Round(worker.SkillsVector.ElementAtOrDefault(s.VectorPosition), 1) })
            .Where(s => s.level > 0)
            .ToList();
        ViewBag.WorkerSkillsJson = JsonSerializer.Serialize(activeSkills);

        ViewBag.PageTitle = "Edit Worker";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Workers", "/Worker/Index"), new("Edit", "") };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        [FromForm] string workerIdEncrypted,
        [FromForm] string name,
        [FromForm] string? role,
        [FromForm] int[]? skillPositions,
        [FromForm] float[]? skillLevels,
        [FromServices] ICommandHandler<UpdateWorkerCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(workerIdEncrypted, out var wid)) return NotFound();
        var vector = new float[1024];
        if (skillPositions is not null && skillLevels is not null)
        {
            int count = Math.Min(skillPositions.Length, skillLevels.Length);
            for (int i = 0; i < count; i++)
            {
                var pos = skillPositions[i];
                if (pos >= 0 && pos < 1024)
                    vector[pos] = Math.Clamp(skillLevels[i], 0.0f, 10.0f);
            }
        }

        await handler.HandleAsync(new UpdateWorkerCommand(wid, name, role ?? string.Empty, vector));
        return RedirectToAction("Detail", new { workerId = workerIdEncrypted });
    }
}
