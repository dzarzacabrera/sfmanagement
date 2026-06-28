using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Infrastructure.Data;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class CreateTaskCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<CreateTaskCommand>
{
    public async Task HandleAsync(CreateTaskCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO tasks (project_id, title, description, criticality, required_skills_vector) " +
            "VALUES ($1, $2, $3, $4::criticality, $5) RETURNING id", connection)
        {
            Parameters =
            {
                new() { Value = command.ProjectId },
                new() { Value = command.Title },
                new() { Value = (object?)command.Description ?? DBNull.Value },
                new() { Value = command.Criticality.ToString().ToLowerInvariant() },
                new() { Value = new Pgvector.Vector(command.RequiredSkillsVector) }
            }
        };

        var result = await cmd.ExecuteScalarAsync();
        command.CreatedId = Convert.ToInt32(result);
    }
}
