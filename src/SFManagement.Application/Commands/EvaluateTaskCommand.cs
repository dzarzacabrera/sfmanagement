using SFManagement.Domain.Enums;

namespace SFManagement.Application.Commands;

public record EvaluateTaskCommand(int TaskId, int WorkerId, IReadOnlyList<SkillEvaluation> Evaluations);

public record SkillEvaluation(int SkillPosition, PerformanceRating Rating);
