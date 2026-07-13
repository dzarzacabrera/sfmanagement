using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class AssignWorkersCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<AssignWorkersCommand>
{
    public async Task HandleAsync(AssignWorkersCommand command)
    {
        if (command.WorkerIds.Count == 0) return;

        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        await using var tx = await connection.BeginTransactionAsync();
        try
        {
            foreach (var workerId in command.WorkerIds)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO task_assignments (task_id, worker_id) VALUES ($1, $2) ON CONFLICT DO NOTHING", connection)
                {
                    Transaction = tx,
                    Parameters =
                    {
                        new() { Value = command.TaskId },
                        new() { Value = workerId }
                    }
                };
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
