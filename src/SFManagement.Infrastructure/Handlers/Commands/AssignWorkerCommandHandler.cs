using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class AssignWorkerCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<AssignWorkerCommand>
{
    public async Task HandleAsync(AssignWorkerCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO task_assignments (task_id, worker_id) VALUES ($1, $2) " +
            "ON CONFLICT (task_id) DO UPDATE SET worker_id = $2, assigned_at = NOW()", connection)
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
