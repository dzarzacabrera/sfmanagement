using Npgsql;
using NpgsqlTypes;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class GetAllTasksQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IGetAllTasksQueryHandler
{
    public async Task<IReadOnlyList<TaskDto>> HandleAsync(GetAllTasksQuery query)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT t.id, t.project_id, t.title, t.description, t.criticality, t.status, " +
            "t.required_skills_vector, p.name AS project_name, " +
            "(SELECT COUNT(DISTINCT pe.worker_id) FROM performance_evaluations pe WHERE pe.task_id = t.id) AS evaluated_count, " +
            "(SELECT COUNT(1) FROM task_assignments ta WHERE ta.task_id = t.id) AS assigned_count " +
            "FROM tasks t " +
            "INNER JOIN projects p ON p.id = t.project_id " +
            "ORDER BY t.project_id DESC, t.status, t.id", connection);

        var tasks = new List<(int Id, int ProjectId, string Title, string? Description,
            Domain.Enums.Criticality Criticality, Domain.Enums.ProjectTaskStatus Status, float[] Vector,
            string ProjectName, bool AllWorkersEvaluated)>();

        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var m = new DataReaderMapper(reader);
                var assignedCount = reader.GetInt32(reader.GetOrdinal("assigned_count"));
                var evaluatedCount = reader.GetInt32(reader.GetOrdinal("evaluated_count"));
                tasks.Add((m.GetInt32("id"), m.GetInt32("project_id"),
                    m.GetString("title"), m.GetStringOrNull("description"),
                    m.GetEnum<Domain.Enums.Criticality>("criticality"),
                    m.GetEnum<Domain.Enums.ProjectTaskStatus>("status"),
                    m.GetVector("required_skills_vector"),
                    m.GetString("project_name"),
                    assignedCount > 0 && evaluatedCount >= assignedCount));
            }
        }

        var assigned = await LoadAssignmentsAsync(connection, tasks.Select(t => t.Id).ToArray());

        return tasks.Select(t => new TaskDto(
            t.Id, t.ProjectId, t.Title, t.Description,
            t.Criticality, t.Status, t.Vector,
            assigned.GetValueOrDefault(t.Id),
            null, t.AllWorkersEvaluated, t.ProjectName)).ToList();
    }

    private static async Task<Dictionary<int, List<AssignedWorkerDto>>> LoadAssignmentsAsync(
        NpgsqlConnection connection, int[] taskIds)
    {
        var result = new Dictionary<int, List<AssignedWorkerDto>>();
        if (taskIds.Length == 0) return result;

        await using var cmd = new NpgsqlCommand(
            "SELECT ta.task_id, w.id AS worker_id, w.name AS worker_name " +
            "FROM task_assignments ta " +
            "INNER JOIN workers w ON w.id = ta.worker_id " +
            "WHERE ta.task_id = ANY($1) " +
            "ORDER BY ta.assigned_at", connection);
        cmd.Parameters.Add(new() { Value = taskIds, NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Integer });

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var m = new DataReaderMapper(reader);
            var taskId = m.GetInt32("task_id");
            if (!result.ContainsKey(taskId))
                result[taskId] = new List<AssignedWorkerDto>();
            result[taskId].Add(new AssignedWorkerDto(
                m.GetInt32("worker_id"), m.GetString("worker_name")));
        }

        return result;
    }
}
