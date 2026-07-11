using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Pgvector;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;
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

    static float[]? ParseFloatArray(string[]? raw)
    {
        if (raw is null) return null;
        var result = new float[raw.Length];
        for (int i = 0; i < raw.Length; i++)
            float.TryParse(raw[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]);
        return result;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromForm] string name,
        [FromForm] string? role,
        [FromForm] int[]? skillPositions,
        [FromForm] string[]? skillLevels,
        [FromServices] ICommandHandler<CreateWorkerCommand> handler)
    {
        var parsed = ParseFloatArray(skillLevels);
        var vector = new float[1024];
        if (skillPositions is not null && parsed is not null)
        {
            int count = Math.Min(skillPositions.Length, parsed.Length);
            for (int i = 0; i < count; i++)
            {
                var pos = skillPositions[i];
                if (pos >= 0 && pos < 1024)
                    vector[pos] = Math.Clamp(parsed[i], 0.0f, 10.0f);
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
        [FromServices] IGetAllSkillsQueryHandler skillsHandler,
        [FromServices] IGetWorkerHistoryQueryHandler historyHandler,
        [FromServices] IGetWorkerTasksQueryHandler tasksHandler,
        [FromServices] IIdEncryptionService enc)
    {
        if (!enc.TryDecrypt(workerId, out var wid)) return NotFound();
        var workers = await allWorkersHandler.HandleAsync(new GetAllWorkersQuery());
        var worker = workers.FirstOrDefault(w => w.Id == wid);
        if (worker is null) return NotFound();

        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());
        ViewBag.AllSkills = skills;
        ViewBag.WorkerSkillsVector = worker.SkillsVector;

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
        var vm = new WorkerHistoryViewModel(wid, worker.Name, evaluations)
        {
            WorkerIdEncrypted = enc.Encrypt(wid)
        };

        var skills = await skillsHandler.HandleAsync(new GetAllSkillsQuery());
        ViewBag.AllSkills = skills;
        ViewBag.WorkerRole = worker.Role;

        var activeSkills = skills
            .Select(s => new { pos = s.VectorPosition, level = Math.Round(worker.SkillsVector.ElementAtOrDefault(s.VectorPosition), 2) })
            .Where(s => s.level > 0)
            .ToList();
        ViewBag.InitialSkillsJson = JsonSerializer.Serialize(activeSkills);

        // Track which skill positions already have evaluations
        ViewBag.EvaluatedSkillPositionsJson = JsonSerializer.Serialize(
            evaluations.Select(e => e.SkillPosition).Distinct().ToArray());

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
        [FromForm] string[]? skillLevels,
        [FromForm] bool confirmedSkillEdit,
        [FromServices] ICommandHandler<UpdateWorkerCommand> handler,
        [FromServices] IGetWorkerHistoryQueryHandler historyHandler,
        [FromServices] IIdEncryptionService enc,
        [FromServices] INpgsqlConnectionFactory connectionFactory)
    {
        if (!enc.TryDecrypt(workerIdEncrypted, out var wid)) return NotFound();

        var parsed = ParseFloatArray(skillLevels);

        // Build new vector
        var vector = new float[1024];
        if (skillPositions is not null && parsed is not null)
        {
            int count = Math.Min(skillPositions.Length, parsed.Length);
            for (int i = 0; i < count; i++)
            {
                var pos = skillPositions[i];
                if (pos >= 0 && pos < 1024)
                    vector[pos] = Math.Clamp(parsed[i], 0.0f, 10.0f);
            }
        }

        // Check if any modified skills have existing evaluations
        var skillPosSet = skillPositions is not null
            ? new HashSet<int>(skillPositions)
            : new HashSet<int>();
        var hasEvaluatedModifiedSkills = false;
        if (skillPosSet.Count > 0)
        {
            var existingEvals = await historyHandler.HandleAsync(new GetWorkerHistoryQuery(wid));
            hasEvaluatedModifiedSkills = existingEvals
                .Select(e => e.SkillPosition)
                .Distinct()
                .Any(p => skillPosSet.Contains(p));
        }

        if (hasEvaluatedModifiedSkills && confirmedSkillEdit)
        {
            // Fetch current worker vector before update
            await using var conn = await connectionFactory.GetOpenConnectionAsync();
            await using var getCmd = new NpgsqlCommand(
                "SELECT skills_vector FROM workers WHERE id = $1", conn);
            getCmd.Parameters.Add(new() { Value = wid });
            var raw = await getCmd.ExecuteScalarAsync() as Vector;
            var oldVector = raw?.ToArray() ?? new float[1024];

            // Get or create the "Manual Adjustment" project and task
            long taskId;
            await using (var findCmd = new NpgsqlCommand(
                "SELECT t.id FROM tasks t " +
                "INNER JOIN projects p ON p.id = t.project_id " +
                "WHERE p.name = 'Manual Adjustment' AND t.title = 'User Skill Edit' " +
                "LIMIT 1", conn))
            {
                var result = await findCmd.ExecuteScalarAsync();
                if (result is long existingTaskId)
                {
                    taskId = existingTaskId;
                }
                else
                {
                    await using var insProj = new NpgsqlCommand(
                        "INSERT INTO projects (name) VALUES ('Manual Adjustment') RETURNING id", conn);
                    var projectId = (long)(await insProj.ExecuteScalarAsync())!;

                    await using var insTask = new NpgsqlCommand(
                        "INSERT INTO tasks (project_id, title, criticality, status, required_skills_vector) " +
                        "VALUES ($1, 'User Skill Edit', 'low', 'Finish', " +
                        "array_fill(0.0::float, ARRAY[1024])::vector(1024)) RETURNING id", conn);
                    insTask.Parameters.Add(new() { Value = projectId });
                    taskId = (long)(await insTask.ExecuteScalarAsync())!;
                }
            }

            // Update worker first
            await handler.HandleAsync(new UpdateWorkerCommand(wid, name, role ?? string.Empty, vector));

            double BasePointsFromDelta(double oldVal, double newVal)
            {
                var diff = newVal - oldVal;
                return Math.Round(diff / 0.05) * 0.05;
            }

            // Create evaluation records for modified skills that had evaluations
            var evaluatedPositions = (await historyHandler.HandleAsync(new GetWorkerHistoryQuery(wid)))
                .Select(e => e.SkillPosition)
                .Distinct()
                .Where(p => skillPosSet.Contains(p))
                .ToHashSet();

            foreach (var pos in skillPosSet)
            {
                if (!evaluatedPositions.Contains(pos)) continue;
                var oldVal = oldVector.ElementAtOrDefault(pos);
                var newVal = vector.ElementAtOrDefault(pos);

                var basePoints = BasePointsFromDelta(oldVal, newVal);

                await using var insEval = new NpgsqlCommand(
                    "INSERT INTO performance_evaluations " +
                    "(task_id, worker_id, skill_position, rating, criticality, base_points, impact, " +
                    "previous_level, new_level) " +
                    "VALUES ($1, $2, $3, $4, $5::criticality, $6, $7, $8, $9)", conn)
                {
                    Parameters =
                    {
                        new() { Value = taskId },
                        new() { Value = wid },
                        new() { Value = pos },
                        new() { Value = newVal },
                        new() { Value = "low" },
                        new() { Value = basePoints },
                        new() { Value = newVal - oldVal },
                        new() { Value = oldVal },
                        new() { Value = newVal }
                    }
                };
                await insEval.ExecuteNonQueryAsync();
            }
        }
        else
        {
            await handler.HandleAsync(new UpdateWorkerCommand(wid, name, role ?? string.Empty, vector));
        }

        return RedirectToAction("Detail", new { workerId = workerIdEncrypted });
    }
}
