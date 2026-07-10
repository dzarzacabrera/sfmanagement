using Npgsql;
using NpgsqlTypes;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Domain.Enums;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Mappers;

namespace SFManagement.Infrastructure.Handlers.Queries;

internal sealed class GetDashboardTasksQueryHandler(INpgsqlConnectionFactory connectionFactory)
    : IGetDashboardTasksQueryHandler
{
    public async Task<IReadOnlyList<TaskDto>> HandleAsync(GetDashboardTasksQuery query)
    {
        await using var connection = await connectionFactory.GetOpenConnectionAsync();

        var skills = await LoadSkillsAsync(connection);

        await using var cmd = new NpgsqlCommand(
            "SELECT t.id, t.project_id, t.title, t.description, t.criticality, t.status, " +
            "t.required_skills_vector, p.name AS project_name, " +
            "(SELECT COUNT(DISTINCT pe.worker_id) FROM performance_evaluations pe WHERE pe.task_id = t.id) AS evaluated_count, " +
            "(SELECT COUNT(1) FROM task_assignments ta WHERE ta.task_id = t.id) AS assigned_count, " +
            "(SELECT COUNT(1) FROM project_workers pw WHERE pw.project_id = t.project_id) AS project_worker_count " +
            "FROM tasks t " +
            "INNER JOIN projects p ON p.id = t.project_id " +
            "WHERE t.project_id = $1 AND t.status != 'Archived' " +
            "ORDER BY t.id", connection);
        cmd.Parameters.Add(new() { Value = query.ProjectId });

        var tasks = new List<(int Id, int ProjectId, string Title, string? Description,
            Criticality Criticality, ProjectTaskStatus Status, float[] Vector, bool AllWorkersEvaluated, string ProjectName, int ProjectWorkerCount)>();

        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var mapper = new DataReaderMapper(reader);
                var assignedCount = reader.GetInt32(reader.GetOrdinal("assigned_count"));
                var evaluatedCount = reader.GetInt32(reader.GetOrdinal("evaluated_count"));
                var projectWorkerCount = reader.GetInt32(reader.GetOrdinal("project_worker_count"));
                tasks.Add((mapper.GetInt32("id"), mapper.GetInt32("project_id"),
                    mapper.GetString("title"), mapper.GetStringOrNull("description"),
                    mapper.GetEnum<Criticality>("criticality"),
                    mapper.GetEnum<ProjectTaskStatus>("status"),
                    mapper.GetVector("required_skills_vector"),
                    assignedCount > 0 && evaluatedCount >= assignedCount,
                    mapper.GetString("project_name"),
                    projectWorkerCount));
            }
        }

        var assigned = await LoadAssignmentsAsync(connection, tasks.Select(t => t.Id).ToArray());

        return tasks.Select(t => new TaskDto(
            t.Id, t.ProjectId, t.Title, t.Description,
            t.Criticality, t.Status, t.Vector,
            assigned.GetValueOrDefault(t.Id),
            DecodeSkills(t.Vector, skills),
            t.AllWorkersEvaluated,
            t.ProjectName,
            t.ProjectWorkerCount > (assigned.GetValueOrDefault(t.Id)?.Count ?? 0))).ToList();
    }

    private static async Task<Dictionary<int, List<AssignedWorkerDto>>> LoadAssignmentsAsync(
        NpgsqlConnection connection, int[] taskIds)
    {
        var result = new Dictionary<int, List<AssignedWorkerDto>>();
        if (taskIds.Length == 0) return result;

        await using var cmd = new NpgsqlCommand(
            "SELECT ta.task_id, w.id AS worker_id, w.name AS worker_name " +
            "FROM task_assignments ta " +
            "INNER JOIN workers w ON w.id = ta.worker_id " +
            "WHERE ta.task_id = ANY($1) " +
            "ORDER BY ta.assigned_at", connection);
        cmd.Parameters.Add(new() { Value = taskIds, NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Integer });

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var m = new DataReaderMapper(reader);
            var taskId = m.GetInt32("task_id");
            if (!result.ContainsKey(taskId))
                result[taskId] = new List<AssignedWorkerDto>();
            result[taskId].Add(new AssignedWorkerDto(
                m.GetInt32("worker_id"), m.GetString("worker_name")));
        }

        return result;
    }

    private static async Task<List<SkillDto>> LoadSkillsAsync(NpgsqlConnection connection)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, vector_position, is_active FROM skills_catalogue ORDER BY vector_position", connection);
        var skills = new List<SkillDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var m = new DataReaderMapper(reader);
            var isActive = reader.GetFieldValue<bool>(reader.GetOrdinal("is_active"));
            skills.Add(new SkillDto(
                m.GetInt32("id"),
                m.GetString("name"),
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
