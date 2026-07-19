using SFManagement.Domain.Enums;

namespace SFManagement.Application.DTOs;

public record EvaluationHistoryDto(
    long Id,
    string TaskTitle,
    string SkillName,
    int SkillPosition,
    double Rating,
    Criticality Criticality,
    double BasePoints,
    double Impact,
    double PreviousLevel,
    double NewLevel,
    DateTime CreatedAt,
    string? ProjectName = null,
    long? TaskId = null)
{
    public const string ManualAdjustmentTitle = "Edit by User";

    public string IdEncrypted { get; init; } = "";
}

public record EvaluationHistoryGroupDto(
    long? TaskId,
    string TaskTitle,
    string ProjectName,
    DateTime EvaluatedAt,
    Criticality Criticality,
    ProjectTaskStatus? Status,
    double AvgScore,
    double TotalImpact,
    int ApprovedSkills,
    int TotalSkills)
{
    public string TaskIdEncrypted { get; init; } = "";
}
