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
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        await using var checkCmd = new NpgsqlCommand(
            "SELECT p.is_finalized FROM tasks t JOIN projects p ON p.id = t.project_id WHERE t.id = $1", connection);
        checkCmd.Parameters.Add(new() { Value = command.TaskId });
        var isFinalized = await checkCmd.ExecuteScalarAsync();
        if (isFinalized is true)
            throw new InvalidOperationException("Cannot assign workers to a task in a closed project.");

        await using var tx = await connection.BeginTransactionAsync();
        try
        {
            // Remove all existing assignments for this task
            await using (var del = new NpgsqlCommand(
                "DELETE FROM task_assignments WHERE task_id = $1", connection)
            {
                Transaction = tx,
                Parameters = { new() { Value = command.TaskId } }
            })
            {
                await del.ExecuteNonQueryAsync();
            }

            // Insert the new set of workers
            foreach (var workerId in command.WorkerIds)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO task_assignments (task_id, worker_id) VALUES ($1, $2)", connection)
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
