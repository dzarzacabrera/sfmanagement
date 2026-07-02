using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Domain.Entities;
using SFManagement.Domain.Enums;
using SFManagement.Domain.ValueObjects;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Commands;

internal sealed class ArchiveTaskCommandHandler(INpgsqlConnectionFactory connectionFactory)
    : ICommandHandler<ArchiveTaskCommand>
{
    public async Task HandleAsync(ArchiveTaskCommand command)
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

        task.ChangeStatus(ProjectTaskStatus.Archived);

        await using var updateCmd = new NpgsqlCommand(
            "UPDATE tasks SET status = $1::task_status WHERE id = $2", connection)
        {
            Parameters =
            {
                new() { Value = "Archived" },
                new() { Value = command.TaskId }
            }
        };

        await updateCmd.ExecuteNonQueryAsync();
    }
}
