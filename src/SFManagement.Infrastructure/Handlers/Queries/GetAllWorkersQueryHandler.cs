using Npgsql;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class GetAllWorkersQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IGetAllWorkersQueryHandler
{
    public async Task<IReadOnlyList<WorkerDto>> HandleAsync(GetAllWorkersQuery query)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT w.id, w.name, w.role, w.skills_vector, " +
            "(SELECT COUNT(*) FROM performance_evaluations WHERE worker_id = w.id) AS evaluation_count, " +
            "(SELECT COUNT(*) FROM task_assignments ta " +
            "INNER JOIN tasks t ON t.id = ta.task_id " +
            "WHERE ta.worker_id = w.id AND t.status NOT IN ('Finish', 'Archived')) AS active_task_count " +
            "FROM workers w ORDER BY w.id", connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        var results = new List<WorkerDto>();
        while (await reader.ReadAsync())
        {
            var mapper = new DataReaderMapper(reader);
            results.Add(new WorkerDto(
                mapper.GetInt32("id"),
                mapper.GetString("name"),
                mapper.GetString("role"),
                mapper.GetInt32("evaluation_count"),
                mapper.GetInt32("active_task_count"),
                mapper.GetVector("skills_vector")));
        }

        return results;
    }
}
