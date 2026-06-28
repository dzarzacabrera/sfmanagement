using Npgsql;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class GetAllSkillsQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IGetAllSkillsQueryHandler
{
    public async Task<List<SkillDto>> HandleAsync(GetAllSkillsQuery query)
    {
        var sql = query.IncludeInactive
            ? "SELECT id, name, vector_position, is_active FROM skills_catalogue ORDER BY vector_position"
            : "SELECT id, name, vector_position, is_active FROM skills_catalogue WHERE is_active = TRUE ORDER BY vector_position";

        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        var results = new List<SkillDto>();
        while (await reader.ReadAsync())
        {
            results.Add(new SkillDto(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetBoolean(3)));
        }

        return results;
    }
}
