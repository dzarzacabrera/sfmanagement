using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class UpdateWorkerCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<UpdateWorkerCommand>
{
    public async Task HandleAsync(UpdateWorkerCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE workers SET name = $1 WHERE id = $2", connection)
        {
            Parameters =
            {
                new() { Value = command.Name },
                new() { Value = command.WorkerId }
            }
        };

        await cmd.ExecuteNonQueryAsync();
    }
}
