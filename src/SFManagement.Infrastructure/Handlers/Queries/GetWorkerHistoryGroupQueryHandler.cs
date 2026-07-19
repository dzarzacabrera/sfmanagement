using Npgsql;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Domain.Enums;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class GetWorkerHistoryGroupQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IGetWorkerHistoryGroupQueryHandler
{
    public async Task<IReadOnlyList<EvaluationHistoryGroupDto>> HandleAsync(GetWorkerHistoryGroupQuery query)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT pe.task_id, " +
            "COALESCE(t.title, 'User Edit') AS task_title, " +
            "COALESCE(p.name, 'Manual Adjustment') AS project_name, " +
            "MAX(pe.created_at) AS evaluated_at, " +
            "(ARRAY_AGG(pe.criticality ORDER BY pe.id DESC))[1] AS criticality, " +
            "t.status, " +
            "ROUND(AVG(pe.rating)::numeric, 2) AS avg_score, " +
            "ROUND(SUM(pe.impact)::numeric, 2) AS total_impact, " +
            "COUNT(*) FILTER (WHERE pe.rating > 0) AS approved_skills, " +
            "COUNT(*) AS total_skills " +
            "FROM performance_evaluations pe " +
            "LEFT JOIN tasks t ON t.id = pe.task_id " +
            "LEFT JOIN projects p ON p.id = t.project_id " +
            "WHERE pe.worker_id = $1 " +
            "GROUP BY pe.task_id, t.title, p.name, t.status " +
            "ORDER BY MAX(pe.created_at) DESC", connection);
        cmd.Parameters.Add(new() { Value = query.WorkerId });

        var results = new List<EvaluationHistoryGroupDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var mapper = new DataReaderMapper(reader);
            results.Add(new EvaluationHistoryGroupDto(
                mapper.GetInt32OrNull("task_id"),
                mapper.GetString("task_title"),
                mapper.GetString("project_name"),
                mapper.GetDateTime("evaluated_at"),
                mapper.GetEnum<Criticality>("criticality"),
                mapper.GetEnumOrNull<ProjectTaskStatus>("status"),
                mapper.GetDouble("avg_score"),
                mapper.GetDouble("total_impact"),
                reader.GetInt32(reader.GetOrdinal("approved_skills")),
                reader.GetInt32(reader.GetOrdinal("total_skills"))));
        }

        return results;
    }
}
