using SFManagement.Domain.Enums;

namespace SFManagement.Application.DTOs;

public record EvaluationHistoryDto(
    int Id,
    string TaskTitle,
    int SkillPosition,
    PerformanceRating Rating,
    Criticality Criticality,
    double BasePoints,
    double Impact,
    double PreviousLevel,
    double NewLevel,
    DateTime CreatedAt);
