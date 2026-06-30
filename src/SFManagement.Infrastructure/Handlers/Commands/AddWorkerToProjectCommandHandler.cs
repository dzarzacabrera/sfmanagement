using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class AddWorkerToProjectCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<AddWorkerToProjectCommand>
{
    public async Task HandleAsync(AddWorkerToProjectCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO project_workers (project_id, worker_id) VALUES ($1, $2) ON CONFLICT DO NOTHING",
            connection);
        cmd.Parameters.Add(new() { Value = command.ProjectId });
        cmd.Parameters.Add(new() { Value = command.WorkerId });
        await cmd.ExecuteNonQueryAsync();
    }
}
