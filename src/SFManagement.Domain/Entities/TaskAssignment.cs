namespace SFManagement.Domain.Entities;

public class TaskAssignment
{
    public long Id { get; init; }
    public long TaskId { get; init; }
    public long WorkerId { get; init; }
    public DateTime AssignedAt { get; init; }

    public TaskAssignment(long id, long taskId, long workerId, DateTime assignedAt)
    {
        Id = id;
        TaskId = taskId;
        WorkerId = workerId;
        AssignedAt = assignedAt;
    }
}
