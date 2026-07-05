using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Web.ViewModels;
using SFManagement.Domain.Enums;
using SFManagement.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SFManagement.Web.Controllers;

public class DashboardController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] int projectId,
        [FromServices] IGetDashboardTasksQueryHandler handler,
        [FromServices] IGetAllProjectsQueryHandler projectsHandler)
    {
        var tasks = await handler.HandleAsync(new GetDashboardTasksQuery(projectId));
        var projects = await projectsHandler.HandleAsync(new GetAllProjectsQuery());
        var projectName = projects.FirstOrDefault(p => p.Id == projectId)?.Name ?? $"Project #{projectId}";

        var vm = new DashboardViewModel(
            projectId,
            projectName,
            tasks.Where(t => t.Status == ProjectTaskStatus.Queued).Select(MapToCard).ToList(),
            tasks.Where(t => t.Status == ProjectTaskStatus.InProgress).Select(MapToCard).ToList(),
            tasks.Where(t => t.Status == ProjectTaskStatus.Blocked).Select(MapToCard).ToList(),
            tasks.Where(t => t.Status == ProjectTaskStatus.Finish).Select(MapToCard).ToList());

        ViewBag.PageTitle = "Dashboard";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Projects", "/Project/Index"), new(projectName, ""), new("Dashboard", "") };

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
        [FromQuery] int? workerId,
        [FromServices] IGetDashboardTasksQueryHandler taskHandler,
        [FromServices] INpgsqlConnectionFactory connFactory)
    {
        var tasks = await taskHandler.HandleAsync(new GetDashboardTasksQuery(projectId));
        var task = tasks.FirstOrDefault(t => t.Id == taskId);
        var workers = task?.AssignedWorkers;

        // Exclude workers that already have evaluations for this task
        HashSet<int> evaluatedWorkerIds = [];
        if (workers?.Count > 0)
        {
            await using var conn = await connFactory.GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT worker_id FROM performance_evaluations WHERE task_id = @taskId";
            cmd.Parameters.AddWithValue("@taskId", taskId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                evaluatedWorkerIds.Add(reader.GetInt32(0));
        }

        var remainingWorkers = workers?.Where(w => !evaluatedWorkerIds.Contains(w.WorkerId)).ToList();

        // Select the requested worker or fall back to first remaining
        var selectedWorker = remainingWorkers?.FirstOrDefault(w => w.WorkerId == (workerId ?? 0))
            ?? remainingWorkers?.FirstOrDefault();

        // Fetch selected worker's skills vector for the recalculation preview
        float[]? currentWorkerVector = null;
        if (selectedWorker != null)
        {
            await using var conn = await connFactory.GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT skills_vector FROM workers WHERE id = @workerId";
            cmd.Parameters.AddWithValue("@workerId", selectedWorker.WorkerId);
            var result = await cmd.ExecuteScalarAsync();
            if (result is Pgvector.Vector vec)
                currentWorkerVector = vec.ToArray();
        }

        var taskSkills = task?.Skills
            ?.Select(s => new SkillPositionDto(s.SkillPosition, s.SkillName))
            .ToList() ?? [];

        var vm = new EvaluationViewModel(
            taskId,
            task?.Title ?? $"Task #{taskId}",
            task?.Description,
            task?.Criticality ?? Criticality.Medium,
            selectedWorker?.WorkerId ?? 0,
            selectedWorker?.WorkerName ?? "Unknown",
            taskSkills,
            remainingWorkers is { Count: > 0 } ? remainingWorkers : null,
            currentWorkerVector);

        return PartialView("_EvaluationModal", vm);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SubmitEvaluation(
        [FromForm] int taskId,
        [FromForm] int workerId,
        [FromForm] int[] skillPositions,
        [FromForm] double[] basePoints,
        [FromServices] ICommandHandler<EvaluateTaskCommand> handler,
        [FromServices] INpgsqlConnectionFactory connFactory)
    {
        var evaluations = skillPositions
            .Select((pos, i) => new SkillEvaluation(pos, basePoints.Length > i ? basePoints[i] : 0.0))
            .ToList();

        await handler.HandleAsync(new EvaluateTaskCommand(taskId, workerId, evaluations));

        // Check if there are remaining unevaluated workers
        var assignedCount = 0;
        var evaluatedCount = 0;
        await using (var conn = await connFactory.GetOpenConnectionAsync())
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT (SELECT COUNT(*) FROM task_assignments WHERE task_id = @taskId) AS total,
                       (SELECT COUNT(DISTINCT worker_id) FROM performance_evaluations WHERE task_id = @taskId) AS evaluated";
            cmd.Parameters.AddWithValue("@taskId", taskId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                assignedCount = reader.GetInt32(0);
                evaluatedCount = reader.GetInt32(1);
            }
        }

        return Json(new { hasMore = evaluatedCount < assignedCount });
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

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ArchiveTask(
        [FromForm] int taskId,
        [FromServices] ICommandHandler<ArchiveTaskCommand> handler)
    {
        await handler.HandleAsync(new ArchiveTaskCommand(taskId));
        return Ok();
    }

    private static TaskCardDto MapToCard(TaskDto t) => new(
        t.Id, t.Title, t.Description, t.Criticality, t.Status,
        t.AssignedWorkers, t.Skills, t.AllWorkersEvaluated);
}
