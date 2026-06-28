using Npgsql;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Domain.Enums;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class GetDashboardTasksQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IGetDashboardTasksQueryHandler
{
    public async Task<IReadOnlyList<TaskDto>> HandleAsync(GetDashboardTasksQuery query)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT t.id, t.project_id, t.title, t.description, t.criticality, t.status, " +
            "t.required_skills_vector, ta.worker_id AS assigned_worker_id, w.name AS assigned_worker_name " +
            "FROM tasks t " +
            "LEFT JOIN task_assignments ta ON ta.task_id = t.id " +
            "LEFT JOIN workers w ON w.id = ta.worker_id " +
            "WHERE t.project_id = $1 " +
            "ORDER BY t.id", connection);
        cmd.Parameters.Add(new() { Value = query.ProjectId });

        var results = new List<TaskDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var mapper = new DataReaderMapper(reader);
            results.Add(new TaskDto(
                mapper.GetInt32("id"),
                mapper.GetInt32("project_id"),
                mapper.GetString("title"),
                mapper.GetStringOrNull("description"),
                mapper.GetEnum<Criticality>("criticality"),
                mapper.GetEnum<ProjectTaskStatus>("status"),
                mapper.GetVector("required_skills_vector"),
                mapper.GetInt32OrNull("assigned_worker_id"),
                mapper.GetStringOrNull("assigned_worker_name")));
        }

        return results;
    }
}
