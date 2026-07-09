using SFManagement.Domain.Enums;

namespace SFManagement.Application.DTOs;

public record TaskSkillDto(string SkillName, int SkillPosition, float RequiredLevel);

public record AssignedWorkerDto(int WorkerId, string WorkerName);

public record TaskDto(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    Criticality Criticality,
    ProjectTaskStatus Status,
    float[] RequiredSkillsVector,
    IReadOnlyList<AssignedWorkerDto>? AssignedWorkers = null,
    IReadOnlyList<TaskSkillDto>? Skills = null,
    bool AllWorkersEvaluated = false,
    string ProjectName = "");
