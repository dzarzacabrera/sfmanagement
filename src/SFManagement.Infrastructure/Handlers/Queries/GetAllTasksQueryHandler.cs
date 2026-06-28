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
            "SELECT id, project_id, title, description, criticality, status, " +
            "required_skills_vector FROM tasks ORDER BY id", connection);

        var tasks = new List<(int Id, int ProjectId, string Title, string? Description,
            Domain.Enums.Criticality Criticality, Domain.Enums.ProjectTaskStatus Status, float[] Vector)>();

        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var m = new DataReaderMapper(reader);
                tasks.Add((m.GetInt32("id"), m.GetInt32("project_id"),
                    m.GetString("title"), m.GetStringOrNull("description"),
                    m.GetEnum<Domain.Enums.Criticality>("criticality"),
                    m.GetEnum<Domain.Enums.ProjectTaskStatus>("status"),
                    m.GetVector("required_skills_vector")));
            }
        }

        var assigned = await LoadAssignmentsAsync(connection, tasks.Select(t => t.Id).ToArray());

        return tasks.Select(t => new TaskDto(
            t.Id, t.ProjectId, t.Title, t.Description,
            t.Criticality, t.Status, t.Vector,
            assigned.GetValueOrDefault(t.Id))).ToList();
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
