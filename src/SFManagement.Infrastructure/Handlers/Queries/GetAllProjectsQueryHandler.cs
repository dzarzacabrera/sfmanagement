using Npgsql;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class GetAllProjectsQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IGetAllProjectsQueryHandler
{
    public async Task<IReadOnlyList<ProjectDto>> HandleAsync(GetAllProjectsQuery query)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT p.id, p.name, p.description_md, p.is_finalized, " +
            "COALESCE((SELECT array_agg(w.name ORDER BY w.id) FROM workers w " +
            "INNER JOIN project_workers pw ON pw.worker_id = w.id WHERE pw.project_id = p.id), " +
            "ARRAY[]::text[]) " +
            "FROM projects p ORDER BY p.id", connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        var results = new List<ProjectDto>();
        while (await reader.ReadAsync())
        {
            results.Add(new ProjectDto(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetFieldValue<string[]>(4),
                reader.GetBoolean(3)));
        }

        return results;
    }
}
