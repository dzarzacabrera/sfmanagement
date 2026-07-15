using Npgsql;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class GetWorkerTasksQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IGetWorkerTasksQueryHandler
{
    public async Task<IReadOnlyList<WorkerTaskDto>> HandleAsync(GetWorkerTasksQuery query)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT t.id AS task_id, t.title AS task_title, p.name AS project_name, p.id AS project_id " +
            "FROM task_assignments ta " +
            "INNER JOIN tasks t ON t.id = ta.task_id " +
            "INNER JOIN projects p ON p.id = t.project_id " +
            "WHERE ta.worker_id = $1 AND t.status NOT IN ('Finish', 'Archived') AND p.is_finalized = FALSE " +
            "ORDER BY t.id", connection);
        cmd.Parameters.Add(new() { Value = query.WorkerId });

        var results = new List<WorkerTaskDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var m = new DataReaderMapper(reader);
            results.Add(new WorkerTaskDto(
                m.GetInt32("task_id"),
                m.GetString("task_title"),
                m.GetString("project_name"),
                m.GetInt32("project_id")));
        }

        return results;
    }
}
