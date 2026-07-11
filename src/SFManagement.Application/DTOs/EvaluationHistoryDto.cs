using SFManagement.Domain.Enums;

namespace SFManagement.Application.DTOs;

public record EvaluationHistoryDto(
    int Id,
    string TaskTitle,
    string SkillName,
    int SkillPosition,
    double Rating,
    Criticality Criticality,
    double BasePoints,
    double Impact,
    double PreviousLevel,
    double NewLevel,
    DateTime CreatedAt)
{
    public string IdEncrypted { get; init; } = "";
}
