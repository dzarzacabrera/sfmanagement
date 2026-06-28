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
        [FromServices] IGetDashboardTasksQueryHandler taskHandler,
        [FromServices] IGetAllSkillsQueryHandler skillsHandler)
    {
        var tasks = await taskHandler.HandleAsync(new GetDashboardTasksQuery(projectId));
        var task = tasks.FirstOrDefault(t => t.Id == taskId);
        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());

        var vm = new EvaluationViewModel(
            taskId,
            task?.Title ?? $"Task #{taskId}",
            task?.AssignedWorkerId ?? 0,
            task?.AssignedWorkerName ?? "Unknown",
            skills.Select(s => new SkillPositionDto(s.VectorPosition, s.Name)).ToList());

        return PartialView("_EvaluationModal", vm);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SubmitEvaluation(
        [FromForm] int taskId,
        [FromForm] int[] skillPositions,
        [FromForm] string[] ratings,
        [FromServices] ICommandHandler<EvaluateTaskCommand> handler)
    {
        var evaluations = skillPositions
            .Select((pos, i) => new SkillEvaluation(pos, Enum.Parse<PerformanceRating>(ratings[i], ignoreCase: true)))
            .ToList();

        await handler.HandleAsync(new EvaluateTaskCommand(taskId, evaluations));
        return Ok();
    }

    private static TaskCardDto MapToCard(TaskDto t) => new(
        t.Id, t.Title, t.Description, t.Criticality, t.Status,
        t.AssignedWorkerId, t.AssignedWorkerName);
}
