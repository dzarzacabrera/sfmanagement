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
        var zeroVector = new Vector(new float[1024]);

        await using var cmd = new NpgsqlCommand(
            "INSERT INTO workers (name, skills_vector) VALUES ($1, $2) RETURNING id", connection)
        {
            Parameters =
            {
                new() { Value = command.Name },
                new() { Value = zeroVector }
            }
        };

        var result = await cmd.ExecuteScalarAsync();
        command.CreatedId = Convert.ToInt32(result);
    }
}
