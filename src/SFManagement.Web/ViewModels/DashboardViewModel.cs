using SFManagement.Application.DTOs;
using SFManagement.Domain.Enums;

namespace SFManagement.Web.ViewModels;

public record DashboardViewModel(
    int ProjectId,
    string ProjectName,
    IReadOnlyList<TaskCardDto> QueuedTasks,
    IReadOnlyList<TaskCardDto> InProgressTasks,
    IReadOnlyList<TaskCardDto> BlockedTasks,
    IReadOnlyList<TaskCardDto> FinishedTasks);

public record TaskCardDto(
    int Id,
    string Title,
    string? Description,
    Criticality Criticality,
    ProjectTaskStatus Status,
    int? AssignedWorkerId,
    string? AssignedWorkerName);

public record AssignWorkerViewModel(
    int TaskId,
    string TaskTitle,
    IReadOnlyList<WorkerScoreDto> Workers);

public record EvaluationViewModel(
    int TaskId,
    string TaskTitle,
    int WorkerId,
    string WorkerName,
    IReadOnlyList<SkillPositionDto> SkillPositions);

public record SkillPositionDto(int Position, string SkillName);

public record WorkerHistoryViewModel(
    int WorkerId,
    string WorkerName,
    IReadOnlyList<EvaluationHistoryDto> Evaluations);
