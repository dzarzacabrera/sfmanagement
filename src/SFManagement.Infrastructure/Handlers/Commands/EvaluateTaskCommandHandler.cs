using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Domain.Entities;
using SFManagement.Domain.Enums;
using SFManagement.Domain.ValueObjects;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class EvaluateTaskCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<EvaluateTaskCommand>
{
    public async Task HandleAsync(EvaluateTaskCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        var (task, assignment) = await LoadTaskAndAssignmentAsync(command.TaskId, command.WorkerId, connection);
        if (task.Status != ProjectTaskStatus.Finish)
            throw new InvalidOperationException("Cannot evaluate a task that is not in Finish status.");

        var requiredVector = task.RequiredSkillsVector.ToArray();
        var requiredPositions = new HashSet<int>();
        for (int i = 0; i < requiredVector.Length; i++)
        {
            if (requiredVector[i] > 0)
                requiredPositions.Add(i);
        }

        var workerSkills = await LoadWorkerSkillsAsync(assignment.WorkerId, connection);
        var currentVector = new SkillVector(workerSkills);
        var hasChanges = false;

        foreach (var evaluation in command.Evaluations)
        {
            if (!requiredPositions.Contains(evaluation.SkillPosition))
                continue;

            var basePoints = evaluation.BasePoints;
            var multiplier = SkillVector.CalculateCriticalityMultiplier(task.Criticality);
            var impact = basePoints * multiplier;
            var previousLevel = currentVector[evaluation.SkillPosition];
            var newVector = currentVector.ApplyImpact(
                evaluation.SkillPosition, basePoints, multiplier);
            var newLevel = newVector[evaluation.SkillPosition];

            if (Math.Abs(newLevel - previousLevel) > 0.001)
            {
                await InsertEvaluationAsync(connection, command.TaskId, assignment.WorkerId,
                    evaluation, task.Criticality, basePoints, impact, previousLevel, newLevel);
                hasChanges = true;
            }

            currentVector = newVector;
        }

        if (hasChanges)
        {
            await UpdateWorkerSkillsAsync(connection, assignment.WorkerId, currentVector);
        }
    }

    private static async Task<(ProjectTask Task, TaskAssignment Assignment)> LoadTaskAndAssignmentAsync(
        int taskId, int workerId, NpgsqlConnection connection)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT t.id, t.project_id, t.title, t.description, t.criticality, t.status, " +
            "t.required_skills_vector, ta.id, ta.task_id, ta.worker_id, ta.assigned_at " +
            "FROM tasks t " +
            "INNER JOIN task_assignments ta ON ta.task_id = t.id " +
            "WHERE t.id = $1 AND ta.worker_id = $2", connection);
        cmd.Parameters.Add(new() { Value = taskId });
        cmd.Parameters.Add(new() { Value = workerId });

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new InvalidOperationException($"Task {taskId} not found or has no assignment.");

        var mapper = new DataReaderMapper(reader);
        var task = new ProjectTask(
            mapper.GetInt32("id"),
            mapper.GetInt32("project_id"),
            mapper.GetString("title"),
            mapper.GetStringOrNull("description"),
            mapper.GetEnum<Criticality>("criticality"),
            mapper.GetEnum<ProjectTaskStatus>("status"),
            new SkillVector(mapper.GetVector("required_skills_vector")));

        var assignment = new TaskAssignment(
            mapper.GetInt32("id"),
            mapper.GetInt32("task_id"),
            mapper.GetInt32("worker_id"),
            mapper.GetDateTime("assigned_at"));

        return (task, assignment);
    }

    private static async Task<float[]> LoadWorkerSkillsAsync(int workerId, NpgsqlConnection connection)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT skills_vector FROM workers WHERE id = $1", connection);
        cmd.Parameters.Add(new() { Value = workerId });

        var result = await cmd.ExecuteScalarAsync() as Pgvector.Vector;
        return result!.ToArray();
    }

    private static string ToPerformanceRatingString(double basePoints) => basePoints switch
    {
        < -0.25 => "Poor",
        < 0.1 => "Average",
        < 0.35 => "Good",
        _ => "Excellent"
    };

    private static async Task InsertEvaluationAsync(NpgsqlConnection connection, int taskId, int workerId,
        SkillEvaluation evaluation, Criticality criticality, double basePoints, double impact,
        double previousLevel, double newLevel)
    {
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO performance_evaluations " +
            "(task_id, worker_id, skill_position, rating, criticality, base_points, impact, " +
            "previous_level, new_level) " +
            "VALUES ($1, $2, $3, $4::performance_rating, $5::criticality, $6, $7, $8, $9)", connection)
        {
            Parameters =
            {
                new() { Value = taskId },
                new() { Value = workerId },
                new() { Value = evaluation.SkillPosition },
                new() { Value = ToPerformanceRatingString(evaluation.BasePoints) },
                new() { Value = criticality.ToString().ToLowerInvariant() },
                new() { Value = Math.Round(basePoints, 2) },
                new() { Value = Math.Round(impact, 2) },
                new() { Value = Math.Round(previousLevel, 1) },
                new() { Value = Math.Round(newLevel, 1) }
            }
        };

        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task UpdateWorkerSkillsAsync(NpgsqlConnection connection, int workerId,
        SkillVector newVector)
    {
        await using var cmd = new NpgsqlCommand(
            "UPDATE workers SET skills_vector = $1 WHERE id = $2", connection)
        {
            Parameters =
            {
                new() { Value = new Pgvector.Vector(newVector.ToArray()) },
                new() { Value = workerId }
            }
        };

        await cmd.ExecuteNonQueryAsync();
    }
}
