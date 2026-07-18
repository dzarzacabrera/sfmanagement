using SFManagement.Domain.Enums;

namespace SFManagement.Application.DTOs;

public record TaskSkillDto(string SkillName, int SkillPosition, float RequiredLevel);

public record AssignedWorkerDto(long WorkerId, string WorkerName, string Role = "")
{
    public string WorkerIdEncrypted { get; init; } = "";
    public int ActiveTaskCount { get; init; }
    public int EvaluationCount { get; init; }
}

public record TaskDto(
    long Id,
    long ProjectId,
    string Title,
    string? Description,
    Criticality Criticality,
    ProjectTaskStatus Status,
    float[] RequiredSkillsVector,
    IReadOnlyList<AssignedWorkerDto>? AssignedWorkers = null,
    IReadOnlyList<TaskSkillDto>? Skills = null,
    bool AllWorkersEvaluated = false,
    string ProjectName = "",
    bool HasAssignableWorkers = true)
{
    public string IdEncrypted { get; init; } = "";
    public string ProjectIdEncrypted { get; init; } = "";
}
