using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;
using SFManagement.Domain.Enums;

namespace SFManagement.Web.ViewModels;

public record DashboardViewModel(
    long ProjectId,
    string ProjectName,
    IReadOnlyList<TaskCardDto> QueuedTasks,
    IReadOnlyList<TaskCardDto> InProgressTasks,
    IReadOnlyList<TaskCardDto> BlockedTasks,
    IReadOnlyList<TaskCardDto> FinishedTasks,
    bool HasProjectWorkers = true,
    bool HasWorkersToAssignToProject = true,
    bool IsFinalized = false)
{
    public string ProjectIdEncrypted { get; init; } = "";
}

public record TaskCardDto(
    long Id,
    string Title,
    string? Description,
    Criticality Criticality,
    ProjectTaskStatus Status,
    IReadOnlyList<AssignedWorkerDto>? AssignedWorkers = null,
    IReadOnlyList<TaskSkillDto>? Skills = null,
    bool AllWorkersEvaluated = false,
    bool HasAssignableWorkers = true)
{
    public string IdEncrypted { get; init; } = "";
}

public record AssignWorkerViewModel(
    long TaskId,
    string TaskTitle,
    string? Description,
    Criticality Criticality,
    IReadOnlyList<TaskSkillDto>? Skills,
    IReadOnlyList<WorkerScoreDto> Workers)
{
    public string TaskIdEncrypted { get; init; } = "";
}

public record EvaluationViewModel(
    long TaskId,
    string TaskTitle,
    string? Description,
    Criticality Criticality,
    long WorkerId,
    string WorkerName,
    IReadOnlyList<SkillPositionDto> SkillPositions,
    IReadOnlyList<AssignedWorkerDto>? AssignedWorkers = null,
    float[]? CurrentWorkerVector = null)
{
    public string TaskIdEncrypted { get; init; } = "";
    public string WorkerIdEncrypted { get; init; } = "";
}

public record SkillPositionDto(int Position, string SkillName, float RequiredLevel);

public record AddWorkerToProjectPopupViewModel(
    long ProjectId,
    IReadOnlyList<WorkerDto> Workers,
    IReadOnlyList<SkillDto> AllSkills)
{
    public string ProjectIdEncrypted { get; init; } = "";
}

public record WorkerHistoryViewModel(
    long WorkerId,
    string WorkerName,
    IReadOnlyList<EvaluationHistoryDto> Evaluations,
    IReadOnlyList<WorkerTaskDto>? AssignedTasks = null)
{
    public string WorkerIdEncrypted { get; init; } = "";
}
