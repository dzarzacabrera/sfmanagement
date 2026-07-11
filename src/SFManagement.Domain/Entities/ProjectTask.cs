using SFManagement.Domain.Enums;
using SFManagement.Domain.ValueObjects;

namespace SFManagement.Domain.Entities;

public class ProjectTask
{
    public long Id { get; init; }
    public long ProjectId { get; init; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public Criticality Criticality { get; private set; }
    public ProjectTaskStatus Status { get; private set; }
    public SkillVector RequiredSkillsVector { get; private set; }

    public ProjectTask(long id, long projectId, string title, string? description,
        Criticality criticality, ProjectTaskStatus status, SkillVector requiredSkillsVector)
    {
        Id = id;
        ProjectId = projectId;
        Title = title;
        Description = description;
        Criticality = criticality;
        Status = status;
        RequiredSkillsVector = requiredSkillsVector;
    }

    public void UpdateDetails(string title, string? description, Criticality criticality, SkillVector requiredSkillsVector)
    {
        if (Status is not (ProjectTaskStatus.Queued or ProjectTaskStatus.InProgress))
            throw new InvalidOperationException("Only queued or in-progress tasks can be edited.");

        Title = title;
        Description = description;
        Criticality = criticality;
        RequiredSkillsVector = requiredSkillsVector;
    }

    public void ChangeStatus(ProjectTaskStatus newStatus)
    {
        // Full freedom between Queued, InProgress, Blocked, Finish
        var isActive = Status is ProjectTaskStatus.Queued or ProjectTaskStatus.InProgress or ProjectTaskStatus.Blocked or ProjectTaskStatus.Finish;
        var isActiveTarget = newStatus is ProjectTaskStatus.Queued or ProjectTaskStatus.InProgress or ProjectTaskStatus.Blocked or ProjectTaskStatus.Finish;
        var validTransitions = (isActive && isActiveTarget)
            || (Status == ProjectTaskStatus.Finish && newStatus == ProjectTaskStatus.Archived)
            || (Status == ProjectTaskStatus.Archived && newStatus == ProjectTaskStatus.Finish);

        if (!validTransitions)
            throw new InvalidOperationException($"Invalid transition from {Status} to {newStatus}.");

        Status = newStatus;
    }
}
