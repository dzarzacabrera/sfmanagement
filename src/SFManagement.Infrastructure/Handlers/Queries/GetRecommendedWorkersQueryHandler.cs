using Npgsql;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class GetRecommendedWorkersQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IGetRecommendedWorkersQueryHandler
{
    public async Task<IReadOnlyList<WorkerScoreDto>> HandleAsync(GetRecommendedWorkersQuery query)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        float[] taskVector;
        await using (var vectorCmd = new NpgsqlCommand(
            "SELECT required_skills_vector FROM tasks WHERE id = $1", connection))
        {
            vectorCmd.Parameters.Add(new() { Value = query.TaskId });
            var raw = await vectorCmd.ExecuteScalarAsync() as Pgvector.Vector;
            taskVector = raw!.ToArray();
        }

        await using var cmd = new NpgsqlCommand(
            "SELECT w.id, w.name, " +
            "(w.skills_vector <#> $1) * -1 AS compatibility_score " +
            "FROM workers w " +
            "INNER JOIN project_workers pw ON pw.worker_id = w.id " +
            "WHERE pw.project_id = $2 " +
            "ORDER BY compatibility_score DESC", connection);
        cmd.Parameters.Add(new() { Value = new Pgvector.Vector(taskVector) });
        cmd.Parameters.Add(new() { Value = query.ProjectId });

        var results = new List<WorkerScoreDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var mapper = new DataReaderMapper(reader);
            results.Add(new WorkerScoreDto(
                mapper.GetInt32("id"),
                mapper.GetString("name"),
                mapper.GetDouble("compatibility_score")));
        }

        return results;
    }
}
