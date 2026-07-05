using Npgsql;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Domain.Enums;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class GetWorkerHistoryQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IGetWorkerHistoryQueryHandler
{
    public async Task<IReadOnlyList<EvaluationHistoryDto>> HandleAsync(GetWorkerHistoryQuery query)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT pe.id, t.title AS task_title, " +
            "COALESCE(sc.name, 'Skill #' || pe.skill_position) AS skill_name, " +
            "pe.skill_position, pe.rating, pe.criticality, " +
            "pe.base_points, pe.impact, pe.previous_level, pe.new_level, pe.created_at " +
            "FROM performance_evaluations pe " +
            "INNER JOIN tasks t ON t.id = pe.task_id " +
            "LEFT JOIN skills_catalogue sc ON sc.vector_position = pe.skill_position " +
            "WHERE pe.worker_id = $1 " +
            "ORDER BY pe.created_at DESC", connection);
        cmd.Parameters.Add(new() { Value = query.WorkerId });

        var results = new List<EvaluationHistoryDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var mapper = new DataReaderMapper(reader);
            results.Add(new EvaluationHistoryDto(
                mapper.GetInt32("id"),
                mapper.GetString("task_title"),
                mapper.GetString("skill_name"),
                mapper.GetInt32("skill_position"),
                mapper.GetEnum<PerformanceRating>("rating"),
                mapper.GetEnum<Criticality>("criticality"),
                mapper.GetDouble("base_points"),
                mapper.GetDouble("impact"),
                mapper.GetDouble("previous_level"),
                mapper.GetDouble("new_level"),
                mapper.GetDateTime("created_at")));
        }

        return results;
    }
}
