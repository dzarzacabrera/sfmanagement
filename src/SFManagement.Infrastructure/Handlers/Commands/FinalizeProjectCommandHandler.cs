using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class FinalizeProjectCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<FinalizeProjectCommand>
{
    public async Task HandleAsync(FinalizeProjectCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        // Check: all tasks must be in Finish status
        await using (var unfinishedCmd = new NpgsqlCommand(
            "SELECT COUNT(1) FROM tasks WHERE project_id = $1 AND status != 'Finish'", connection))
        {
            unfinishedCmd.Parameters.Add(new() { Value = command.ProjectId });
            var unfinishedCount = (long)(await unfinishedCmd.ExecuteScalarAsync() ?? 0);
            if (unfinishedCount > 0)
                throw new InvalidOperationException(
                    "Please finish all tasks and evaluate all workers to close the project.");
        }

        // Check: all finished tasks with assigned workers must have all workers evaluated
        await using (var unassessedCmd = new NpgsqlCommand(@"
            SELECT COUNT(1) FROM tasks t
            WHERE t.project_id = $1 AND t.status = 'Finish'
            AND (SELECT COUNT(1) FROM task_assignments ta WHERE ta.task_id = t.id) > 0
            AND (SELECT COUNT(DISTINCT pe.worker_id) FROM performance_evaluations pe WHERE pe.task_id = t.id)
                < (SELECT COUNT(1) FROM task_assignments ta WHERE ta.task_id = t.id)", connection))
        {
            unassessedCmd.Parameters.Add(new() { Value = command.ProjectId });
            var unassessedCount = (long)(await unassessedCmd.ExecuteScalarAsync() ?? 0);
            if (unassessedCount > 0)
                throw new InvalidOperationException(
                    "Please finish all tasks and evaluate all workers to close the project.");
        }

        // All conditions met — finalize the project
        await using var finalizeCmd = new NpgsqlCommand(
            "UPDATE projects SET is_finalized = TRUE WHERE id = $1", connection);
        finalizeCmd.Parameters.Add(new() { Value = command.ProjectId });
        await finalizeCmd.ExecuteNonQueryAsync();
    }
}
