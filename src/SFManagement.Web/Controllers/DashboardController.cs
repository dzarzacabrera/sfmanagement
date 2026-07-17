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
            tasks.Where(t => t.Status == ProjectTaskStatus.Test).Select(t => MapToCard(t, enc)).ToList(),
            tasks.Where(t => t.Status == ProjectTaskStatus.Finish).Select(t => MapToCard(t, enc)).ToList(),
            hasProjectWorkers,
            hasWorkersToAssignToProject,
            project?.IsFinalized ?? false)
        {
            ProjectIdEncrypted = enc.Encrypt(pid)
        };

        ViewBag.PageTitle = "Dashboard";
        ViewBag.Breadcrumbs = new List<KeyValuePair<string, string>> { new("Projects", "/Project/Index"), new(projectName, "") };

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

        var workersList = workers.Select(w => w with { IdEncrypted = enc.Encrypt(w.Id) }).ToList();
        var remaining = new List<WorkerScoreDto>(workersList);

        WorkerScoreDto? mostEfficient = null;
        WorkerScoreDto? growUp = null;
        WorkerScoreDto? fastestToFinish = null;

        var taskSkills = task?.Skills ?? new List<TaskSkillDto>();

        // 1. Most Efficient: 95-100%, not exceeds, pick lowest in range (closest to 95%)
        var efficientCandidates = remaining
            .Where(w => w.CompatibilityScore >= 0.95 && w.CompatibilityScore <= 1.00 && !w.Exceeds)
            .OrderBy(w => w.CompatibilityScore)
            .ToList();
        if (efficientCandidates.Count != 0)
        {
            mostEfficient = efficientCandidates[0];
            remaining.Remove(mostEfficient);
        }

        // 2. Grow Up: 75-95%, closest to 87%
        var growUpCandidates = remaining
            .Where(w => w.CompatibilityScore >= 0.75 && w.CompatibilityScore <= 0.95)
            .OrderBy(w => Math.Abs(w.CompatibilityScore - 0.87))
            .ToList();
        if (growUpCandidates.Count != 0)
        {
            growUp = growUpCandidates[0];
            remaining.Remove(growUp);
        }

        // 3. Fastest to Finish: 100%, exceeds, smallest total excess
        var fastestCandidates = remaining
            .Where(w => w.CompatibilityScore >= 1.00 && w.Exceeds)
            .ToList();
        if (fastestCandidates.Count != 0)
        {
            fastestToFinish = fastestCandidates
                .OrderBy(w => ComputeTotalExcess(w, taskSkills))
                .First();
            remaining.Remove(fastestToFinish);
        }

        var vm = new AssignWorkerViewModel(
            tid,
            task?.Title ?? $"Task #{tid}",
            task?.Description,
            task?.Criticality ?? Criticality.Medium,
            taskSkills,
            remaining)
        {
            TaskIdEncrypted = enc.Encrypt(tid),
            MostEfficient = mostEfficient,
            GrowUp = growUp,
            FastestToFinish = fastestToFinish
        };

        return PartialView("_AssignWorkerModal", vm);
    }

    private static double ComputeTotalExcess(WorkerScoreDto worker, IReadOnlyList<TaskSkillDto> taskSkills)
    {
        double total = 0;
        foreach (var skill in taskSkills)
        {
            if (skill.SkillPosition < worker.SkillsVector.Length)
            {
                var excess = Math.Max(0, worker.SkillsVector[skill.SkillPosition] - skill.RequiredLevel);
                total += excess;
            }
        }
        return total;
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
    public async Task<IActionResult> AssignWorkers(
        [FromForm] string taskIdEncrypted,
        [FromForm] string[]? workerIdsEncrypted,
        [FromServices] ICommandHandler<AssignWorkersCommand> handler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(taskIdEncrypted, out var tid)) return BadRequest();
        var workerIds = new List<long>();
        if (workerIdsEncrypted != null)
        {
            foreach (var wEnc in workerIdsEncrypted)
            {
                if (enc.TryDecrypt(wEnc, out var wid))
                    workerIds.Add(wid);
            }
        }
        await handler.HandleAsync(new AssignWorkersCommand(tid, workerIds));
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
            ?.Select(s => new SkillPositionDto(s.SkillPosition, s.SkillName, s.RequiredLevel))
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
            ProjectIdEncrypted = enc.Encrypt(pid)
        };
        return PartialView("_AddWorkerToProjectModal", vm);
    }

    [HttpGet]
    public async Task<IActionResult> CreateTaskPopup(
        [FromQuery] string projectId,
        [FromServices] IGetAllProjectsQueryHandler projectsHandler,
        [FromServices] IGetAllSkillsQueryHandler skillsHandler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(projectId, out var pid)) return NotFound();
        var projects = (await projectsHandler.HandleAsync(new GetAllProjectsQuery())).Where(p => !p.IsFinalized).ToList();
        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());
        var criticalities = new List<CriticalityOption>
        {
            new(Criticality.Low, "Low"),
            new(Criticality.Medium, "Medium"),
            new(Criticality.High, "High"),
            new(Criticality.Critical, "Critical"),
        };

        ViewBag.AllSkills = skills;

        var vm = new CreateTaskViewModel(pid,
            projects.Select(p => p with { IdEncrypted = enc.Encrypt(p.Id) }).ToList(),
            skills.Select(s => new SkillCatalogueItem(s.Id, s.Name, s.VectorPosition) { IdEncrypted = enc.Encrypt(s.Id) }).ToList(),
            criticalities)
        {
            ProjectIdEncrypted = enc.Encrypt(pid)
        };
        return PartialView("_CreateTaskModal", vm);
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
