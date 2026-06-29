using Npgsql;
using Pgvector;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class CreateWorkerCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<CreateWorkerCommand>
{
    public async Task HandleAsync(CreateWorkerCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(
            "INSERT INTO workers (name, role, skills_vector) VALUES ($1, $2, $3) RETURNING id", connection)
        {
            Parameters =
            {
                new() { Value = command.Name },
                new() { Value = command.Role },
                new() { Value = new Pgvector.Vector(command.SkillsVector) }
            }
        };

        var result = await cmd.ExecuteScalarAsync();
        command.CreatedId = Convert.ToInt32(result);
    }
}
