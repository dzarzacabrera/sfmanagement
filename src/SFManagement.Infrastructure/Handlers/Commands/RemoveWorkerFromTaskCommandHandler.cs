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

        await using var guardCmd = new NpgsqlCommand(
            "SELECT p.is_finalized FROM tasks t JOIN projects p ON p.id = t.project_id WHERE t.id = $1", connection);
        guardCmd.Parameters.Add(new() { Value = command.TaskId });
        var isFinalized = await guardCmd.ExecuteScalarAsync();
        if (isFinalized is true)
            throw new InvalidOperationException("Cannot remove workers from a task in a closed project.");

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
