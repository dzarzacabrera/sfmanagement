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
        var (_, taskId) = await CreateProjectWithQueuedTaskAsync(scope.ServiceProvider);

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateTaskCommand>>();
        var command = new UpdateTaskCommand(taskId, "Updated Title", "Updated",
            Criticality.Low, new float[12]);

        await handler.HandleAsync(command);
    }

    [Fact]
    public async Task UpdateInProgressTask_ShouldThrow()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var (_, taskId) = await CreateProjectWithQueuedTaskAsync(scope.ServiceProvider);

        var statusHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ChangeTaskStatusCommand>>();
        await statusHandler.HandleAsync(new ChangeTaskStatusCommand(taskId, ProjectTaskStatus.InProgress));

        var updateHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateTaskCommand>>();
        var command = new UpdateTaskCommand(taskId, "Should Fail", null,
            Criticality.Medium, new float[12]);

        await FluentActions.Invoking(() => updateHandler.HandleAsync(command))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateFinishedTask_ShouldThrow()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var (_, taskId) = await CreateProjectWithQueuedTaskAsync(scope.ServiceProvider);

        var statusHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ChangeTaskStatusCommand>>();
        await statusHandler.HandleAsync(new ChangeTaskStatusCommand(taskId, ProjectTaskStatus.InProgress));
        await statusHandler.HandleAsync(new ChangeTaskStatusCommand(taskId, ProjectTaskStatus.Finish));

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateTaskCommand>>();
        var command = new UpdateTaskCommand(taskId, "Should Fail", null,
            Criticality.Medium, new float[12]);

        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    private static async Task<(int ProjectId, int TaskId)> CreateProjectWithQueuedTaskAsync(IServiceProvider services)
    {
        var createProject = services.GetRequiredService<ICommandHandler<CreateProjectCommand>>();
        var projectCmd = new CreateProjectCommand("Edit Test Project", null);
        await createProject.HandleAsync(projectCmd);

        var createTask = services.GetRequiredService<ICommandHandler<CreateTaskCommand>>();
        var taskCmd = new CreateTaskCommand(projectCmd.CreatedId, "Test Task", null,
            Criticality.Medium, new float[12]);
        await createTask.HandleAsync(taskCmd);

        return (projectCmd.CreatedId, taskCmd.CreatedId);
    }
}
