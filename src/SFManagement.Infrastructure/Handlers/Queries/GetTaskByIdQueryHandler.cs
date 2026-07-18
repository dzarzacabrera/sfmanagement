using Npgsql;
using NpgsqlTypes;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class GetTaskByIdQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IGetTaskByIdQueryHandler
{
    public async Task<TaskDto?> HandleAsync(GetTaskByIdQuery query)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT t.id, t.project_id, t.title, t.description, t.criticality, t.status, " +
            "t.required_skills_vector, p.name AS project_name " +
            "FROM tasks t " +
            "INNER JOIN projects p ON p.id = t.project_id " +
            "WHERE t.id = $1", connection);
        cmd.Parameters.Add(new() { Value = query.TaskId });

        TaskDto? task = null;
        float[] vector = [];
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                var m = new DataReaderMapper(reader);
                vector = m.GetVector("required_skills_vector");
                task = new TaskDto(
                    m.GetInt32("id"), m.GetInt32("project_id"),
                    m.GetString("title"), m.GetStringOrNull("description"),
                    m.GetEnum<Domain.Enums.Criticality>("criticality"),
                    m.GetEnum<Domain.Enums.ProjectTaskStatus>("status"),
                    vector,
                    ProjectName: m.GetString("project_name"));
            }
        }

        if (task is null) return null;

        var catalogue = await LoadSkillsAsync(connection);
        var skills = DecodeSkills(vector, catalogue);
        var assigned = await LoadAssignmentsAsync(connection, task.Id);
        return task with { AssignedWorkers = assigned, Skills = skills };

    }

    private static async Task<List<AssignedWorkerDto>?> LoadAssignmentsAsync(
        NpgsqlConnection connection, long taskId)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT w.id AS worker_id, w.name AS worker_name, w.role AS worker_role " +
            "FROM task_assignments ta " +
            "INNER JOIN workers w ON w.id = ta.worker_id " +
            "WHERE ta.task_id = $1 " +
            "ORDER BY ta.assigned_at", connection);
        cmd.Parameters.Add(new() { Value = taskId });

        var result = new List<AssignedWorkerDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var m = new DataReaderMapper(reader);
            result.Add(new AssignedWorkerDto(
                m.GetInt32("worker_id"), m.GetString("worker_name"), m.GetString("worker_role")));
        }

        return result.Count > 0 ? result : null;
    }

    private static async Task<List<SkillDto>> LoadSkillsAsync(NpgsqlConnection connection)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, description, vector_position, is_active FROM skills_catalogue ORDER BY vector_position", connection);
        var skills = new List<SkillDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var m = new DataReaderMapper(reader);
            var isActive = reader.GetFieldValue<bool>(reader.GetOrdinal("is_active"));
            skills.Add(new SkillDto(
                m.GetInt32("id"),
                m.GetString("name"),
                m.GetString("description"),
                m.GetInt32("vector_position"),
                isActive));
        }
        return skills;
    }

    private static List<TaskSkillDto> DecodeSkills(float[] vector, List<SkillDto> catalogue)
    {
        var result = new List<TaskSkillDto>();
        for (int i = 0; i < vector.Length; i++)
        {
            if (vector[i] > 0)
            {
                var skill = catalogue.FirstOrDefault(s => s.VectorPosition == i);
                var name = skill?.Name ?? $"Skill #{i}";
                result.Add(new TaskSkillDto(name, i, vector[i]));
            }
        }
        return result;
    }
}
