using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class RemoveWorkerFromTaskCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<RemoveWorkerFromTaskCommand>
{
    public async Task HandleAsync(RemoveWorkerFromTaskCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "DELETE FROM task_assignments WHERE task_id = $1 AND worker_id = $2", connection)
        {
            Parameters =
            {
                new() { Value = command.TaskId },
                new() { Value = command.WorkerId }
            }
        };

        await cmd.ExecuteNonQueryAsync();
    }
}
