using SFManagement.Domain.Enums;

namespace SFManagement.Application.Commands;

public record EvaluateTaskCommand(int TaskId, IReadOnlyList<SkillEvaluation> Evaluations);

public record SkillEvaluation(int SkillPosition, PerformanceRating Rating);
