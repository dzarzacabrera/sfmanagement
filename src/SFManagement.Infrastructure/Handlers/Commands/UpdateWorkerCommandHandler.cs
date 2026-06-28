using Npgsql;
using Pgvector;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class UpdateWorkerCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<UpdateWorkerCommand>
{
    public async Task HandleAsync(UpdateWorkerCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        if (command.SkillsVector is not null)
        {
            await using var cmd = new NpgsqlCommand(
                "UPDATE workers SET name = $1, role = $2, skills_vector = $3 WHERE id = $4", connection)
            {
                Parameters =
                {
                    new() { Value = command.Name },
                    new() { Value = command.Role },
                    new() { Value = new Vector(command.SkillsVector) },
                    new() { Value = command.WorkerId }
                }
            };
            await cmd.ExecuteNonQueryAsync();
        }
        else
        {
            await using var cmd = new NpgsqlCommand(
                "UPDATE workers SET name = $1, role = $2 WHERE id = $3", connection)
            {
                Parameters =
                {
                    new() { Value = command.Name },
                    new() { Value = command.Role },
                    new() { Value = command.WorkerId }
                }
            };
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
