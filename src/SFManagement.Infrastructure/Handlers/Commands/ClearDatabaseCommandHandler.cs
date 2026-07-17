using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class ClearDatabaseCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<ClearDatabaseCommand>
{
    public async Task HandleAsync(ClearDatabaseCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        await using var truncateCmd = new NpgsqlCommand(@"
            TRUNCATE TABLE
                performance_evaluations,
                task_assignments,
                tasks,
                project_workers,
                projects,
                workers,
                skills_catalogue
            RESTART IDENTITY CASCADE;
        ", connection);
        await truncateCmd.ExecuteNonQueryAsync();
    }
}
