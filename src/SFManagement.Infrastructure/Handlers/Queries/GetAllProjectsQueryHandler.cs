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
            "SELECT id, name, description_md FROM projects ORDER BY id", connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        var results = new List<ProjectDto>();
        while (await reader.ReadAsync())
        {
            results.Add(new ProjectDto(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2)));
        }

        return results;
    }
}
