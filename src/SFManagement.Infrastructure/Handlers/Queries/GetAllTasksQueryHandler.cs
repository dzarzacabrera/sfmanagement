using Npgsql;
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
            "t.required_skills_vector, a.worker_id, w.name AS worker_name " +
            "FROM tasks t " +
            "LEFT JOIN task_assignments a ON a.task_id = t.id " +
            "LEFT JOIN workers w ON w.id = a.worker_id " +
            "ORDER BY t.id", connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        var results = new List<TaskDto>();
        while (await reader.ReadAsync())
        {
            var mapper = new DataReaderMapper(reader);
            results.Add(new TaskDto(
                mapper.GetInt32("id"),
                mapper.GetInt32("project_id"),
                mapper.GetString("title"),
                mapper.GetStringOrNull("description"),
                mapper.GetEnum<Domain.Enums.Criticality>("criticality"),
                mapper.GetEnum<Domain.Enums.ProjectTaskStatus>("status"),
                mapper.GetVector("required_skills_vector"),
                reader.IsDBNull(7) ? null : mapper.GetInt32("worker_id"),
                reader.IsDBNull(8) ? null : mapper.GetString("worker_name")));
        }

        return results;
    }
}
