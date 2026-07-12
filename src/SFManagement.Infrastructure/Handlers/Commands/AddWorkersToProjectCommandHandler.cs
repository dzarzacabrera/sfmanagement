using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class AddWorkersToProjectCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<AddWorkersToProjectCommand>
{
    public async Task HandleAsync(AddWorkersToProjectCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        foreach (var wid in command.WorkerIds)
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO project_workers (project_id, worker_id) VALUES ($1, $2) ON CONFLICT DO NOTHING",
                connection);
            cmd.Parameters.Add(new() { Value = command.ProjectId });
            cmd.Parameters.Add(new() { Value = wid });
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
