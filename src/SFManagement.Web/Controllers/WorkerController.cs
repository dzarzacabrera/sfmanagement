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
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromForm] string name,
        [FromServices] ICommandHandler<CreateWorkerCommand> handler)
    {
        var command = new CreateWorkerCommand(name);
        await handler.HandleAsync(command);
        return RedirectToAction("Edit", new { workerId = command.CreatedId });
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromServices] IGetAllWorkersQueryHandler handler,
        [FromServices] IGetAllSkillsQueryHandler skillsHandler)
    {
        var workers = await handler.HandleAsync(new GetAllWorkersQuery());
        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());
        ViewBag.AllSkills = skills;
        return View(workers);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(
        [FromQuery] int workerId,
        [FromServices] IGetAllWorkersQueryHandler allWorkersHandler,
        [FromServices] IGetWorkerHistoryQueryHandler handler)
    {
        var workers = await allWorkersHandler.HandleAsync(new GetAllWorkersQuery());
        var worker = workers.FirstOrDefault(w => w.Id == workerId);
        if (worker is null) return NotFound();

        var evaluations = await handler.HandleAsync(new GetWorkerHistoryQuery(workerId));

        var vm = new WorkerHistoryViewModel(
            workerId,
            worker.Name,
            evaluations);

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
