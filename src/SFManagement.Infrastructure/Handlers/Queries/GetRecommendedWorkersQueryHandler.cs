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
            taskVector = raw?.ToArray() ?? [];
        }

        var taskSelfDot = taskVector.Sum(v => v * v);
        if (taskSelfDot == 0) taskSelfDot = 1;

        await using var cmd = new NpgsqlCommand(
            "SELECT w.id, w.name, w.role, " +
            "(w.skills_vector <#> $1) * -1 AS raw_score, " +
            "w.skills_vector, " +
            "CASE WHEN ta.worker_id IS NOT NULL THEN true ELSE false END AS is_assigned " +
            "FROM workers w " +
            "INNER JOIN project_workers pw ON pw.worker_id = w.id " +
            "LEFT JOIN task_assignments ta ON ta.worker_id = w.id AND ta.task_id = $3 " +
            "WHERE pw.project_id = $2 " +
            "ORDER BY is_assigned DESC, raw_score DESC", connection);
        cmd.Parameters.Add(new() { Value = new Pgvector.Vector(taskVector) });
        cmd.Parameters.Add(new() { Value = query.ProjectId });
        cmd.Parameters.Add(new() { Value = query.TaskId });

        var results = new List<WorkerScoreDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var mapper = new DataReaderMapper(reader);
            var rawScore = reader.GetDouble(reader.GetOrdinal("raw_score"));
            var isAssigned = reader.GetBoolean(reader.GetOrdinal("is_assigned"));
            var normalizedScore = rawScore / taskSelfDot;
            results.Add(new WorkerScoreDto(
                mapper.GetInt32("id"),
                mapper.GetString("name"),
                mapper.GetString("role"),
                normalizedScore,
                mapper.GetVector("skills_vector"),
                isAssigned));
        }

        return results;
    }
}
