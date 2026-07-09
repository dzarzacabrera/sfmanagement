using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Domain.Entities;
using SFManagement.Domain.Enums;
using SFManagement.Domain.ValueObjects;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class UpdateTaskCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<UpdateTaskCommand>
{
    public async Task HandleAsync(UpdateTaskCommand command)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        ProjectTask task;
        await using (var loadCmd = new NpgsqlCommand(
            "SELECT id, project_id, title, description, criticality, status, required_skills_vector " +
            "FROM tasks WHERE id = $1", connection))
        {
            loadCmd.Parameters.Add(new() { Value = command.TaskId });
            await using var reader = await loadCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException($"Task with id {command.TaskId} not found.");

            var mapper = new DataReaderMapper(reader);
            task = new ProjectTask(
                mapper.GetInt32("id"),
                mapper.GetInt32("project_id"),
                mapper.GetString("title"),
                mapper.GetStringOrNull("description"),
                mapper.GetEnum<Criticality>("criticality"),
                mapper.GetEnum<ProjectTaskStatus>("status"),
                new SkillVector(mapper.GetVector("required_skills_vector")));
        }

        task.UpdateDetails(command.Title, command.Description, command.Criticality,
            new SkillVector(command.RequiredSkillsVector));

        await using var updateCmd = new NpgsqlCommand(
            "UPDATE tasks SET title = $1, description = $2, criticality = $3::criticality, " +
            "required_skills_vector = $4, project_id = $5 WHERE id = $6", connection)
        {
            Parameters =
            {
                new() { Value = command.Title },
                new() { Value = (object?)command.Description ?? DBNull.Value },
                new() { Value = command.Criticality.ToString().ToLowerInvariant() },
                new() { Value = new Pgvector.Vector(command.RequiredSkillsVector) },
                new() { Value = command.ProjectId },
                new() { Value = command.TaskId }
            }
        };

        await updateCmd.ExecuteNonQueryAsync();
    }
}
