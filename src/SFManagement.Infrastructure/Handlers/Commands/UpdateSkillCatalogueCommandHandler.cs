using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class UpdateSkillCatalogueCommandHandler(
    INpgsqlConnectionFactory connectionFactory,
    IIsSkillUsedQueryHandler usedHandler)
    : ICommandHandler<UpdateSkillCatalogueCommand>
{
    public async Task HandleAsync(UpdateSkillCatalogueCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        await using var posCmd = new NpgsqlCommand(
            "SELECT vector_position FROM skills_catalogue WHERE id = $1", connection)
        {
            Parameters = { new() { Value = command.SkillId } }
        };
        var posResult = await posCmd.ExecuteScalarAsync();
        if (posResult is null) return;
        var vectorPosition = (int)posResult;

        if (await usedHandler.HandleAsync(new IsSkillUsedQuery(vectorPosition)))
            throw new InvalidOperationException("Cannot edit a skill that has been used in evaluations or assignments.");

        await using var cmd = new NpgsqlCommand(
            "UPDATE skills_catalogue SET name = $1, description = $2 WHERE id = $3", connection)
        {
            Parameters =
            {
                new() { Value = command.Name },
                new() { Value = command.Description },
                new() { Value = command.SkillId }
            }
        };

        await cmd.ExecuteNonQueryAsync();
    }
}
