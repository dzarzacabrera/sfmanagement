using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class UpdateProjectCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<UpdateProjectCommand>
{
    public async Task HandleAsync(UpdateProjectCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE projects SET name = $1, description_md = $2 WHERE id = $3", connection)
        {
            Parameters =
            {
                new() { Value = command.Name },
                new() { Value = (object?)command.DescriptionMd ?? DBNull.Value },
                new() { Value = command.ProjectId }
            }
        };

        await cmd.ExecuteNonQueryAsync();
    }
}
