using FluentAssertions;
using SFManagement.Domain.Entities;
using SFManagement.Domain.Enums;
using SFManagement.Domain.ValueObjects;

namespace SFManagement.UnitTests;

public class DomainValidationTests
{
    private static ProjectTask CreateQueuedTask() =>
        new(1, 1, "Test Task", null, Criticality.Medium, ProjectTaskStatus.Queued,
            new SkillVector([1.0f, 2.0f]));

    [Fact]
    public void ProjectTask_UpdateDetails_WhenQueued_Succeeds()
    {
        var task = CreateQueuedTask();
        var newVector = new SkillVector([8.0f, 8.0f]);

        task.UpdateDetails("New Title", "New desc", Criticality.High, newVector);

        task.Title.Should().Be("New Title");
        task.Criticality.Should().Be(Criticality.High);
    }

    [Fact]
    public void ProjectTask_UpdateDetails_WhenInProgress_Throws()
    {
        var task = CreateQueuedTask();
        task.ChangeStatus(ProjectTaskStatus.InProgress);

        Action act = () => task.UpdateDetails("x", null, Criticality.Low, new SkillVector([1.0f]));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*queued*");
    }

    [Fact]
    public void ProjectTask_ChangeStatus_QueuedToInProgress_Succeeds()
    {
        var task = CreateQueuedTask();

        task.ChangeStatus(ProjectTaskStatus.InProgress);

        task.Status.Should().Be(ProjectTaskStatus.InProgress);
    }

    [Fact]
    public void ProjectTask_ChangeStatus_InProgressToBlocked_Succeeds()
    {
        var task = CreateQueuedTask();
        task.ChangeStatus(ProjectTaskStatus.InProgress);

        task.ChangeStatus(ProjectTaskStatus.Blocked);

        task.Status.Should().Be(ProjectTaskStatus.Blocked);
    }

    [Fact]
    public void ProjectTask_ChangeStatus_InProgressToFinish_Succeeds()
    {
        var task = CreateQueuedTask();
        task.ChangeStatus(ProjectTaskStatus.InProgress);

        task.ChangeStatus(ProjectTaskStatus.Finish);

        task.Status.Should().Be(ProjectTaskStatus.Finish);
    }

    [Fact]
    public void ProjectTask_ChangeStatus_BlockedToQueued_Succeeds()
    {
        var task = CreateQueuedTask();
        task.ChangeStatus(ProjectTaskStatus.InProgress);
        task.ChangeStatus(ProjectTaskStatus.Blocked);

        task.ChangeStatus(ProjectTaskStatus.Queued);

        task.Status.Should().Be(ProjectTaskStatus.Queued);
    }

    [Fact]
    public void ProjectTask_ChangeStatus_FinishToAny_Throws()
    {
        var task = CreateQueuedTask();
        task.ChangeStatus(ProjectTaskStatus.InProgress);
        task.ChangeStatus(ProjectTaskStatus.Finish);

        Action act = () => task.ChangeStatus(ProjectTaskStatus.Queued);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Finished*");
    }

    [Fact]
    public void ProjectTask_ChangeStatus_InvalidTransition_Throws()
    {
        var task = CreateQueuedTask();

        Action act = () => task.ChangeStatus(ProjectTaskStatus.Finish);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SkillCatalogue_UpdateName_ChangesName()
    {
        var skill = new SkillCatalogue(1, "JavaScript", 0);

        skill.UpdateName("TypeScript");

        skill.Name.Should().Be("TypeScript");
    }

    [Fact]
    public void Worker_UpdateName_ChangesName()
    {
        var worker = new Worker(1, "Oriol", new SkillVector([1.0f]));

        worker.UpdateName("Oriol Updated");

        worker.Name.Should().Be("Oriol Updated");
    }

    [Fact]
    public void Project_UpdateDetails_ChangesValues()
    {
        var project = new Project(1, "Old Name", "Old desc");

        project.UpdateDetails("New Name", "New desc");

        project.Name.Should().Be("New Name");
        project.DescriptionMd.Should().Be("New desc");
    }
}
