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
        [FromQuery] string? projectId,
        [FromServices] IGetDashboardTasksQueryHandler handler,
        [FromServices] IGetAllProjectsQueryHandler projectsHandler,
        [FromServices] IIdEncryptionService enc,
        [FromServices] INpgsqlConnectionFactory connFactory)
    {
        if (projectId == null || !enc.TryDecrypt(projectId, out var pid))
        {
            if (!long.TryParse(projectId, out pid)) pid = 1;
        }
        var tasks = await handler.HandleAsync(new GetDashboardTasksQuery(pid));
        var projects = await projectsHandler.HandleAsync(new GetAllProjectsQuery());
        var project = projects.FirstOrDefault(p => p.Id == pid);
        var projectName = project?.Name ?? $"Project #{pid}";

        var hasProjectWorkers = true;
        var hasWorkersToAssignToProject = true;
        await using (var conn = await connFactory.GetOpenConnectionAsync())
        {
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(1) FROM project_workers WHERE project_id = $1";
                cmd.Parameters.Add(new() { Value = pid });
                var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
                hasProjectWorkers = count > 0;
            }
            await using (var cmd2 = conn.CreateCommand())
            {
                cmd2.CommandText = "SELECT COUNT(1) FROM workers WHERE id NOT IN (SELECT worker_id FROM project_workers WHERE project_id = $1)";
                cmd2.Parameters.Add(new() { Value = pid });
                var available = (long)(await cmd2.ExecuteScalarAsync() ?? 0);
                hasWorkersToAssignToProject = available > 0;
            }
        }

        var vm = new DashboardViewModel(
            pid, projectName,
            tasks.Where(t => t.Status == ProjectTaskStatus.Queued).Select(t => MapToCard(t, enc)).ToList(),
            tasks.Where(t => t.Status == ProjectTaskStatus.InProgress).Select(t => MapToCard(t, enc)).ToList(),
            tasks.Where(t => t.Status == ProjectTaskStatus.Blocked).Select(t => MapToCard(t, enc)).ToList(),
            tasks.Where(t => t.Status == ProjectTaskStatus.Finish).Select(t => MapToCard(t, enc)).ToList(),
            hasProjectWorkers,
            hasWorkersToAssignToProject)
        {
            ProjectIdEncrypted = enc.Encrypt(pid)
        };

        ViewBag.PageTitle = "Dashboard";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Projects", "/Project/Index"), new(projectName, ""), new("Dashboard", "") };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> AssignPopup(
        [FromQuery] string taskId,
        [FromQuery] string projectId,
        [FromServices] IGetRecommendedWorkersQueryHandler handler,
        [FromServices] IGetDashboardTasksQueryHandler taskHandler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(taskId, out var tid)) return NotFound();
        if (!enc.TryDecrypt(projectId, out var pid)) return NotFound();

        var workers = await handler.HandleAsync(new GetRecommendedWorkersQuery(pid, tid));
        var tasks = await taskHandler.HandleAsync(new GetDashboardTasksQuery(pid));
        var task = tasks.FirstOrDefault(t => t.Id == tid);

        var vm = new AssignWorkerViewModel(
            tid,
            task?.Title ?? $"Task #{tid}",
            task?.Description,
            task?.Criticality ?? Criticality.Medium,
            task?.Skills ?? new List<TaskSkillDto>(),
            workers.Select(w => w with { IdEncrypted = enc.Encrypt(w.Id) }).ToList())
        {
            TaskIdEncrypted = enc.Encrypt(tid)
        };

        return PartialView("_AssignWorkerModal", vm);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AssignWorker(
        [FromForm] string taskIdEncrypted,
        [FromForm] string workerIdEncrypted,
        [FromServices] ICommandHandler<AssignWorkerCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(taskIdEncrypted, out var tid)) return BadRequest();
        if (!enc.TryDecrypt(workerIdEncrypted, out var wid)) return BadRequest();
        await handler.HandleAsync(new AssignWorkerCommand(tid, wid));
        return Ok();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RemoveWorker(
        [FromForm] string taskIdEncrypted,
        [FromForm] string workerIdEncrypted,
        [FromServices] ICommandHandler<RemoveWorkerFromTaskCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(taskIdEncrypted, out var tid)) return BadRequest();
        if (!enc.TryDecrypt(workerIdEncrypted, out var wid)) return BadRequest();
        await handler.HandleAsync(new RemoveWorkerFromTaskCommand(tid, wid));
        return Ok();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ChangeStatus(
        [FromForm] string taskIdEncrypted,
        [FromForm] string newStatus,
        [FromServices] ICommandHandler<ChangeTaskStatusCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(taskIdEncrypted, out var tid)) return BadRequest();
        var status = Enum.Parse<ProjectTaskStatus>(newStatus, ignoreCase: true);
        await handler.HandleAsync(new ChangeTaskStatusCommand(tid, status));
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> EvaluationPopup(
        [FromQuery] string taskId,
        [FromQuery] string projectId,
        [FromQuery] string? workerId,
        [FromServices] IGetDashboardTasksQueryHandler taskHandler,
        [FromServices] INpgsqlConnectionFactory connFactory,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(taskId, out var tid)) return NotFound();
        if (!enc.TryDecrypt(projectId, out var pid)) return NotFound();

        var tasks = await taskHandler.HandleAsync(new GetDashboardTasksQuery(pid));
        var task = tasks.FirstOrDefault(t => t.Id == tid);
        var workers = task?.AssignedWorkers;

        HashSet<long> evaluatedWorkerIds = [];
        if (workers?.Count > 0)
        {
            await using var conn = await connFactory.GetOpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT worker_id FROM performance_evaluations WHERE task_id = @taskId";
            cmd.Parameters.AddWithValue("@taskId", tid);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                evaluatedWorkerIds.Add(reader.GetInt64(0));
        }

        var remainingWorkers = workers?.Where(w => !evaluatedWorkerIds.Contains(w.WorkerId)).ToList();

        var requestedWorkerId = workerId != null && enc.TryDecrypt(workerId, out var rwid) ? rwid : 0;
        var selectedWorker = remainingWorkers?.FirstOrDefault(w => w.WorkerId == requestedWorkerId)
            ?? remainingWorkers?.FirstOrDefault();

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

        var encryptedWorkers = remainingWorkers?
            .Select(w => w with { WorkerIdEncrypted = enc.Encrypt(w.WorkerId) })
            .ToList();

        var vm = new EvaluationViewModel(
            tid,
            task?.Title ?? $"Task #{tid}",
            task?.Description,
            task?.Criticality ?? Criticality.Medium,
            selectedWorker?.WorkerId ?? 0,
            selectedWorker?.WorkerName ?? "Unknown",
            taskSkills,
            encryptedWorkers is { Count: > 0 } ? encryptedWorkers : null,
            currentWorkerVector)
        {
            TaskIdEncrypted = enc.Encrypt(tid),
            WorkerIdEncrypted = enc.Encrypt(selectedWorker?.WorkerId ?? 0)
        };

        return PartialView("_EvaluationModal", vm);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SubmitEvaluation(
        [FromForm] string taskIdEncrypted,
        [FromForm] string workerIdEncrypted,
        [FromForm] int[] skillPositions,
        [FromForm] int[] basePoints,
        [FromServices] ICommandHandler<EvaluateTaskCommand> handler,
        [FromServices] INpgsqlConnectionFactory connFactory,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(taskIdEncrypted, out var tid)) return BadRequest();
        if (!enc.TryDecrypt(workerIdEncrypted, out var wid)) return BadRequest();

        var evaluations = skillPositions
            .Select((pos, i) => new SkillEvaluation(pos, basePoints.Length > i ? basePoints[i] / 10000.0 : 0.0))
            .ToList();

        await handler.HandleAsync(new EvaluateTaskCommand(tid, wid, evaluations));

        var assignedCount = 0;
        var evaluatedCount = 0;
        await using (var conn = await connFactory.GetOpenConnectionAsync())
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT (SELECT COUNT(*) FROM task_assignments WHERE task_id = @taskId) AS total,
                       (SELECT COUNT(DISTINCT worker_id) FROM performance_evaluations WHERE task_id = @taskId) AS evaluated";
            cmd.Parameters.AddWithValue("@taskId", tid);
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
        [FromQuery] string projectId,
        [FromServices] IGetWorkersNotInProjectQueryHandler handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(projectId, out var pid)) return NotFound();
        var workers = await handler.HandleAsync(new GetWorkersNotInProjectQuery(pid));
        var vm = new AddWorkerToProjectPopupViewModel(pid,
            workers.Select(w => w with { IdEncrypted = enc.Encrypt(w.Id) }).ToList())
        {
            ProjectIdEncrypted = enc.Encrypt(pid)
        };
        return PartialView("_AddWorkerToProjectModal", vm);
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

    [HttpGet]
    public async Task<IActionResult> GetTaskCardHtml(
        [FromQuery] string taskId,
        [FromQuery] string projectId,
        [FromServices] IGetDashboardTasksQueryHandler handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(taskId, out var tid)) return NotFound();
        if (!enc.TryDecrypt(projectId, out var pid)) return NotFound();

        var tasks = await handler.HandleAsync(new GetDashboardTasksQuery(pid));
        var task = tasks.FirstOrDefault(t => t.Id == tid);
        if (task == null) return NotFound();

        ViewBag.ProjectIdEncrypted = enc.Encrypt(pid);
        return PartialView("_TaskCard", MapToCard(task, enc));
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ArchiveTask(
        [FromForm] string taskIdEncrypted,
        [FromServices] ICommandHandler<ArchiveTaskCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(taskIdEncrypted, out var tid)) return BadRequest();
        await handler.HandleAsync(new ArchiveTaskCommand(tid));
        return Ok();
    }

    private static TaskCardDto MapToCard(TaskDto t, IIdEncryptionService enc) => new(
        t.Id, t.Title, t.Description, t.Criticality, t.Status,
        t.AssignedWorkers?.Select(w => w with { WorkerIdEncrypted = enc.Encrypt(w.WorkerId) }).ToList(),
        t.Skills, t.AllWorkersEvaluated, t.HasAssignableWorkers)
    {
        IdEncrypted = enc.Encrypt(t.Id)
    };
}
