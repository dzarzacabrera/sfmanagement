public record EvaluateTaskCommand(int TaskId, int WorkerId, IReadOnlyList<SkillEvaluation> Evaluations);

public record SkillEvaluation(int SkillPosition, double BasePoints);
