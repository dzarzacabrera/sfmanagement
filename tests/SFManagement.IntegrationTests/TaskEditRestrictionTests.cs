using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Domain.Enums;

namespace SFManagement.IntegrationTests;

[Collection("Database")]
public sealed class TaskEditRestrictionTests(SfManagementFixture fixture)
{
    [Fact]
    public async Task UpdateQueuedTask_ShouldSucceed()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var (projectId, taskId) = await CreateProjectWithQueuedTaskAsync(scope.ServiceProvider);

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateTaskCommand>>();
        var command = new UpdateTaskCommand(taskId, projectId, "Updated Title", "Updated",
            Criticality.Low, new float[1024]);

        await handler.HandleAsync(command);
    }

    [Fact]
    public async Task UpdateInProgressTask_ShouldSucceed()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var (projectId, taskId) = await CreateProjectWithQueuedTaskAsync(sp);

        var assignHandler = sp.GetRequiredService<ICommandHandler<AssignWorkerCommand>>();
        await assignHandler.HandleAsync(new AssignWorkerCommand(taskId, 1));

        var statusHandler = sp.GetRequiredService<ICommandHandler<ChangeTaskStatusCommand>>();
        await statusHandler.HandleAsync(new ChangeTaskStatusCommand(taskId, ProjectTaskStatus.InProgress));

        var updateHandler = sp.GetRequiredService<ICommandHandler<UpdateTaskCommand>>();
        var command = new UpdateTaskCommand(taskId, projectId, "Updated Title", null,
            Criticality.Medium, new float[1024]);

        await updateHandler.HandleAsync(command);
    }

    [Fact]
    public async Task UpdateFinishedTask_ShouldThrow()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var (projectId, taskId) = await CreateProjectWithQueuedTaskAsync(sp);

        var assignHandler = sp.GetRequiredService<ICommandHandler<AssignWorkerCommand>>();
        await assignHandler.HandleAsync(new AssignWorkerCommand(taskId, 1));

        var statusHandler = sp.GetRequiredService<ICommandHandler<ChangeTaskStatusCommand>>();
        await statusHandler.HandleAsync(new ChangeTaskStatusCommand(taskId, ProjectTaskStatus.InProgress));
        await statusHandler.HandleAsync(new ChangeTaskStatusCommand(taskId, ProjectTaskStatus.Finish));

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateTaskCommand>>();
        var command = new UpdateTaskCommand(taskId, projectId, "Should Fail", null,
            Criticality.Medium, new float[1024]);

        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    private static async Task<(long ProjectId, long TaskId)> CreateProjectWithQueuedTaskAsync(IServiceProvider services)
    {
        var createProject = services.GetRequiredService<ICommandHandler<CreateProjectCommand>>();
        var projectCmd = new CreateProjectCommand("Edit Test Project", null);
        await createProject.HandleAsync(projectCmd);

        var createTask = services.GetRequiredService<ICommandHandler<CreateTaskCommand>>();
        var skills = new float[1024];
        skills[0] = 1.0f;
        var taskCmd = new CreateTaskCommand(projectCmd.CreatedId, "Test Task", null,
            Criticality.Medium, skills);
        await createTask.HandleAsync(taskCmd);

        return (projectCmd.CreatedId, taskCmd.CreatedId);
    }
}
