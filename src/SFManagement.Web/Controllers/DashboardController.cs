using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Web.ViewModels;
using SFManagement.Domain.Enums;

namespace SFManagement.Web.Controllers;

public class DashboardController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] int projectId,
        [FromServices] IGetDashboardTasksQueryHandler handler)
    {
        var tasks = await handler.HandleAsync(new GetDashboardTasksQuery(projectId));

        var vm = new DashboardViewModel(
            projectId,
            $"Project #{projectId}",
            tasks.Where(t => t.Status == ProjectTaskStatus.Queued).Select(MapToCard).ToList(),
            tasks.Where(t => t.Status == ProjectTaskStatus.InProgress).Select(MapToCard).ToList(),
            tasks.Where(t => t.Status == ProjectTaskStatus.Blocked).Select(MapToCard).ToList(),
            tasks.Where(t => t.Status == ProjectTaskStatus.Finish).Select(MapToCard).ToList());

        ViewBag.PageTitle = "Dashboard";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Projects", "/Project/Index"), new($"Project #{projectId}", ""), new("Dashboard", "") };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> AssignPopup(
        [FromQuery] int taskId,
        [FromQuery] int projectId,
        [FromServices] IGetRecommendedWorkersQueryHandler handler,
        [FromServices] IGetDashboardTasksQueryHandler taskHandler)
    {
        var workers = await handler.HandleAsync(new GetRecommendedWorkersQuery(projectId, taskId));
        var tasks = await taskHandler.HandleAsync(new GetDashboardTasksQuery(projectId));
        var task = tasks.FirstOrDefault(t => t.Id == taskId);

        var vm = new AssignWorkerViewModel(
            taskId,
            task?.Title ?? $"Task #{taskId}",
            task?.Criticality ?? Criticality.Medium,
            task?.Skills ?? new List<TaskSkillDto>(),
            workers);

        return PartialView("_AssignWorkerModal", vm);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AssignWorker(
        [FromForm] int taskId,
        [FromForm] int workerId,
        [FromServices] ICommandHandler<AssignWorkerCommand> handler)
    {
        await handler.HandleAsync(new AssignWorkerCommand(taskId, workerId));
        return Ok();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RemoveWorker(
        [FromForm] int taskId,
        [FromForm] int workerId,
        [FromServices] ICommandHandler<RemoveWorkerFromTaskCommand> handler)
    {
        await handler.HandleAsync(new RemoveWorkerFromTaskCommand(taskId, workerId));
        return Ok();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ChangeStatus(
        [FromForm] int taskId,
        [FromForm] string newStatus,
        [FromServices] ICommandHandler<ChangeTaskStatusCommand> handler)
    {
        var status = Enum.Parse<ProjectTaskStatus>(newStatus, ignoreCase: true);
        await handler.HandleAsync(new ChangeTaskStatusCommand(taskId, status));
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> EvaluationPopup(
        [FromQuery] int taskId,
        [FromQuery] int projectId,
        [FromServices] IGetDashboardTasksQueryHandler taskHandler)
    {
        var tasks = await taskHandler.HandleAsync(new GetDashboardTasksQuery(projectId));
        var task = tasks.FirstOrDefault(t => t.Id == taskId);
        var workers = task?.AssignedWorkers;

        var taskSkills = task?.Skills
            ?.Select(s => new SkillPositionDto(s.SkillPosition, s.SkillName))
            .ToList() ?? [];

        var vm = new EvaluationViewModel(
            taskId,
            task?.Title ?? $"Task #{taskId}",
            workers?.FirstOrDefault()?.WorkerId ?? 0,
            workers?.FirstOrDefault()?.WorkerName ?? "Unknown",
            taskSkills,
            workers);

        return PartialView("_EvaluationModal", vm);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SubmitEvaluation(
        [FromForm] int taskId,
        [FromForm] int workerId,
        [FromForm] int[] skillPositions,
        [FromForm] string[] ratings,
        [FromServices] ICommandHandler<EvaluateTaskCommand> handler)
    {
        var evaluations = skillPositions
            .Select((pos, i) => new SkillEvaluation(pos, Enum.Parse<PerformanceRating>(ratings[i], ignoreCase: true)))
            .ToList();

        await handler.HandleAsync(new EvaluateTaskCommand(taskId, workerId, evaluations));
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> AddWorkerPopup(
        [FromQuery] int projectId,
        [FromServices] IGetWorkersNotInProjectQueryHandler handler)
    {
        var workers = await handler.HandleAsync(new GetWorkersNotInProjectQuery(projectId));
        var vm = new AddWorkerToProjectPopupViewModel(projectId, workers);
        return PartialView("_AddWorkerToProjectModal", vm);
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

    [HttpGet]
    public async Task<IActionResult> GetTaskCardHtml(
        [FromQuery] int taskId,
        [FromQuery] int projectId,
        [FromServices] IGetDashboardTasksQueryHandler handler)
    {
        var tasks = await handler.HandleAsync(new GetDashboardTasksQuery(projectId));
        var task = tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null) return NotFound();

        ViewBag.ProjectId = projectId;
        return PartialView("_TaskCard", MapToCard(task));
    }

    private static TaskCardDto MapToCard(TaskDto t) => new(
        t.Id, t.Title, t.Description, t.Criticality, t.Status,
        t.AssignedWorkers, t.Skills);
}
