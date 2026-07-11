public record EvaluateTaskCommand(long TaskId, long WorkerId, IReadOnlyList<SkillEvaluation> Evaluations);

public record SkillEvaluation(int SkillPosition, double BasePoints);
