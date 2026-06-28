using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class ToggleSkillActiveCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<ToggleSkillActiveCommand>
{
    public async Task HandleAsync(ToggleSkillActiveCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE skills_catalogue SET is_active = NOT is_active WHERE id = $1", connection)
        {
            Parameters = { new() { Value = command.SkillId } }
        };
        await cmd.ExecuteNonQueryAsync();
    }
}
