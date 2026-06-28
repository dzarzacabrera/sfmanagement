using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class UpdateSkillCatalogueCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<UpdateSkillCatalogueCommand>
{
    public async Task HandleAsync(UpdateSkillCatalogueCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE skills_catalogue SET name = $1 WHERE id = $2", connection)
        {
            Parameters =
            {
                new() { Value = command.Name },
                new() { Value = command.SkillId }
            }
        };

        await cmd.ExecuteNonQueryAsync();
    }
}
