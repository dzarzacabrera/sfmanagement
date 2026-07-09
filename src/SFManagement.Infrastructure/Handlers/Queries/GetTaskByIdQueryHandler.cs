using Npgsql;
using NpgsqlTypes;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class GetTaskByIdQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IGetTaskByIdQueryHandler
{
    public async Task<TaskDto?> HandleAsync(GetTaskByIdQuery query)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT t.id, t.project_id, t.title, t.description, t.criticality, t.status, " +
            "t.required_skills_vector, p.name AS project_name " +
            "FROM tasks t " +
            "INNER JOIN projects p ON p.id = t.project_id " +
            "WHERE t.id = $1", connection);
        cmd.Parameters.Add(new() { Value = query.TaskId });

        TaskDto? task = null;
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                var m = new DataReaderMapper(reader);
                var vector = m.GetVector("required_skills_vector");
                task = new TaskDto(
                    m.GetInt32("id"), m.GetInt32("project_id"),
                    m.GetString("title"), m.GetStringOrNull("description"),
                    m.GetEnum<Domain.Enums.Criticality>("criticality"),
                    m.GetEnum<Domain.Enums.ProjectTaskStatus>("status"),
                    vector,
                    ProjectName: m.GetString("project_name"));
            }
        }

        if (task is null) return null;

        var assigned = await LoadAssignmentsAsync(connection, task.Id);
        return task with { AssignedWorkers = assigned };

    }

    private static async Task<List<AssignedWorkerDto>?> LoadAssignmentsAsync(
        NpgsqlConnection connection, int taskId)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT w.id AS worker_id, w.name AS worker_name " +
            "FROM task_assignments ta " +
            "INNER JOIN workers w ON w.id = ta.worker_id " +
            "WHERE ta.task_id = $1 " +
            "ORDER BY ta.assigned_at", connection);
        cmd.Parameters.Add(new() { Value = taskId });

        var result = new List<AssignedWorkerDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var m = new DataReaderMapper(reader);
            result.Add(new AssignedWorkerDto(
                m.GetInt32("worker_id"), m.GetString("worker_name")));
        }

        return result.Count > 0 ? result : null;
    }
}
