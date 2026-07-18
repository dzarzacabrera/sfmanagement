using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class ClearDatabaseCommandHandler(
    INpgsqlConnectionFactory connectionFactory,
    RecyclableNpgsqlDataSource dataSource)
    : ICommandHandler<ClearDatabaseCommand>
{
    public async Task HandleAsync(ClearDatabaseCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        await using var dropCmd = new NpgsqlCommand(@"
            DROP TABLE IF EXISTS
                performance_evaluations,
                task_assignments,
                tasks,
                project_workers,
                projects,
                workers,
                skills_catalogue
            CASCADE;

            DROP TYPE IF EXISTS task_status;
            DROP TYPE IF EXISTS criticality;
        ", connection);
        await dropCmd.ExecuteNonQueryAsync();

        dataSource.Recycle();
    }
}
