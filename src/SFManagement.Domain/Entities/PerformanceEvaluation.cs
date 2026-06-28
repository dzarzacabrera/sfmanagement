using SFManagement.Domain.Enums;

namespace SFManagement.Domain.Entities;

public class PerformanceEvaluation
{
    public int Id { get; init; }
    public int TaskId { get; init; }
    public int WorkerId { get; init; }
    public int SkillPosition { get; init; }
    public PerformanceRating Rating { get; init; }
    public Criticality Criticality { get; init; }
    public double BasePoints { get; init; }
    public double Impact { get; init; }
    public double PreviousLevel { get; init; }
    public double NewLevel { get; init; }
    public DateTime CreatedAt { get; init; }

    public PerformanceEvaluation(int id, int taskId, int workerId, int skillPosition,
        PerformanceRating rating, Criticality criticality, double basePoints,
        double impact, double previousLevel, double newLevel, DateTime createdAt)
    {
        Id = id;
        TaskId = taskId;
        WorkerId = workerId;
        SkillPosition = skillPosition;
        Rating = rating;
        Criticality = criticality;
        BasePoints = basePoints;
        Impact = impact;
        PreviousLevel = previousLevel;
        NewLevel = newLevel;
        CreatedAt = createdAt;
    }
}
