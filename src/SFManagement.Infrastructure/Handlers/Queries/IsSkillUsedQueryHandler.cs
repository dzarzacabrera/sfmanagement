using Npgsql;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class IsSkillUsedQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IIsSkillUsedQueryHandler
{
    public async Task<bool> HandleAsync(IsSkillUsedQuery query)
    {
        var used = await GetUsedPositionsAsync();
        return used.Contains(query.VectorPosition);
    }

    public async Task<HashSet<int>> GetUsedPositionsAsync()
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        await using var evalCmd = new NpgsqlCommand(
            "SELECT DISTINCT skill_position FROM performance_evaluations", connection);
        var used = new HashSet<int>();
        await using (var reader = await evalCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                used.Add(reader.GetInt32(0));
        }

        var catalogue = new List<(int id, int pos)>();
        await using (var catCmd = new NpgsqlCommand(
            "SELECT id, vector_position FROM skills_catalogue", connection))
        await using (var reader = await catCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                catalogue.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }

        foreach (var (_, pos) in catalogue)
        {
            if (used.Contains(pos)) continue;
            var idx = pos + 1;
            await using var workerCmd = new NpgsqlCommand(
                $"SELECT 1 FROM workers WHERE (skills_vector::real[])[{idx}] != 0 LIMIT 1", connection);
            if (await workerCmd.ExecuteScalarAsync() is not null)
            {
                used.Add(pos);
                continue;
            }
            await using var taskCmd = new NpgsqlCommand(
                $"SELECT 1 FROM tasks WHERE (required_skills_vector::real[])[{idx}] != 0 LIMIT 1", connection);
            if (await taskCmd.ExecuteScalarAsync() is not null)
                used.Add(pos);
        }

        return used;
    }
}
