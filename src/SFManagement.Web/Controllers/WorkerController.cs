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
        [FromServices] IGetAllSkillsQueryHandler skillsHandler)
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
        [FromServices] IGetAllSkillsQueryHandler skillsHandler)
    {
        var workers = await handler.HandleAsync(new GetAllWorkersQuery());
        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());
        ViewBag.AllSkills = skills;
        ViewBag.PageTitle = "Workers";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Workers", "") };
        return View(workers);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(
        [FromQuery] int workerId,
        [FromServices] IGetAllWorkersQueryHandler allWorkersHandler,
        [FromServices] IGetWorkerHistoryQueryHandler historyHandler,
        [FromServices] IGetWorkerTasksQueryHandler tasksHandler)
    {
        var workers = await allWorkersHandler.HandleAsync(new GetAllWorkersQuery());
        var worker = workers.FirstOrDefault(w => w.Id == workerId);
        if (worker is null) return NotFound();

        var evaluations = await historyHandler.HandleAsync(new GetWorkerHistoryQuery(workerId));
        var tasks = await tasksHandler.HandleAsync(new GetWorkerTasksQuery(workerId));

        var vm = new WorkerHistoryViewModel(
            workerId,
            worker.Name,
            evaluations,
            tasks);

        ViewBag.PageTitle = worker.Name;
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Workers", "/Worker/Index"), new("Detail", "") };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        [FromQuery] int workerId,
        [FromServices] IGetAllWorkersQueryHandler allWorkersHandler,
        [FromServices] IGetAllSkillsQueryHandler skillsHandler,
        [FromServices] IGetWorkerHistoryQueryHandler historyHandler)
    {
        var workers = await allWorkersHandler.HandleAsync(new GetAllWorkersQuery());
        var worker = workers.FirstOrDefault(w => w.Id == workerId);
        if (worker is null) return NotFound();

        var evaluations = await historyHandler.HandleAsync(new GetWorkerHistoryQuery(workerId));
        var vm = new WorkerHistoryViewModel(workerId, worker.Name, evaluations);

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
        [FromForm] int workerId,
        [FromForm] string name,
        [FromForm] string? role,
        [FromForm] int[]? skillPositions,
        [FromForm] float[]? skillLevels,
        [FromServices] ICommandHandler<UpdateWorkerCommand> handler)
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

        await handler.HandleAsync(new UpdateWorkerCommand(workerId, name, role ?? string.Empty, vector));
        return RedirectToAction("Detail", new { workerId });
    }
}
