using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class CreateSkillCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<CreateSkillCommand>
{
    public async Task HandleAsync(CreateSkillCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "CALL sp_add_skill($1, null)", connection)
        {
            Parameters = { new() { Value = command.Name } }
        };
        await cmd.ExecuteNonQueryAsync();
    }
}
