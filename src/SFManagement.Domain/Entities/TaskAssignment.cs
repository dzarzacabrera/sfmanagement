namespace SFManagement.Domain.Entities;

public class TaskAssignment
{
    public int Id { get; init; }
    public int TaskId { get; init; }
    public int WorkerId { get; init; }
    public DateTime AssignedAt { get; init; }

    public TaskAssignment(int id, int taskId, int workerId, DateTime assignedAt)
    {
        Id = id;
        TaskId = taskId;
        WorkerId = workerId;
        AssignedAt = assignedAt;
    }
}
