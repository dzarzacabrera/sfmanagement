using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class CreateProjectCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<CreateProjectCommand>
{
    public async Task HandleAsync(CreateProjectCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO projects (name, description_md) VALUES ($1, $2) RETURNING id", connection)
        {
            Parameters =
            {
                new() { Value = command.Name },
                new() { Value = (object?)command.DescriptionMd ?? DBNull.Value }
            }
        };

        var result = await cmd.ExecuteScalarAsync();
        command.CreatedId = Convert.ToInt32(result);
    }
}
