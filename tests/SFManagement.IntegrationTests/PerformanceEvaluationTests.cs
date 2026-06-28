using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Domain.Enums;

namespace SFManagement.IntegrationTests;

[Collection("Database")]
public sealed class PerformanceEvaluationTests(SfManagementFixture fixture)
{
    [Fact]
    public async Task FullEvaluationFlow_ShouldUpdateWorkerSkill()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var (_, taskId) = await CreateFinishedTaskAsync(sp);

        var evalHandler = sp.GetRequiredService<ICommandHandler<EvaluateTaskCommand>>();
        var evalCommand = new EvaluateTaskCommand(taskId,
            [new SkillEvaluation(0, PerformanceRating.Excellent)]);
        await evalHandler.HandleAsync(evalCommand);
    }

    [Fact]
    public async Task EvaluateNonFinishedTask_ShouldThrow()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var (_, taskId) = await CreateInProgressTaskAsync(sp);

        var evalHandler = sp.GetRequiredService<ICommandHandler<EvaluateTaskCommand>>();
        var evalCommand = new EvaluateTaskCommand(taskId,
            [new SkillEvaluation(0, PerformanceRating.Average)]);

        await FluentActions.Invoking(() => evalHandler.HandleAsync(evalCommand))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    private static async Task<(int ProjectId, int TaskId)> CreateInProgressTaskAsync(IServiceProvider services)
    {
        var (projectId, taskId) = await CreateProjectWithQueuedTaskAsync(services);

        var assignHandler = services.GetRequiredService<ICommandHandler<AssignWorkerCommand>>();
        await assignHandler.HandleAsync(new AssignWorkerCommand(taskId, 1));

        var statusHandler = services.GetRequiredService<ICommandHandler<ChangeTaskStatusCommand>>();
        await statusHandler.HandleAsync(new ChangeTaskStatusCommand(taskId, ProjectTaskStatus.InProgress));

        return (projectId, taskId);
    }

    private static async Task<(int ProjectId, int TaskId)> CreateFinishedTaskAsync(IServiceProvider services)
    {
        var (projectId, taskId) = await CreateInProgressTaskAsync(services);

        var statusHandler = services.GetRequiredService<ICommandHandler<ChangeTaskStatusCommand>>();
        await statusHandler.HandleAsync(new ChangeTaskStatusCommand(taskId, ProjectTaskStatus.Finish));

        return (projectId, taskId);
    }

    private static async Task<(int ProjectId, int TaskId)> CreateProjectWithQueuedTaskAsync(IServiceProvider services)
    {
        var createProject = services.GetRequiredService<ICommandHandler<CreateProjectCommand>>();
        var projectCmd = new CreateProjectCommand("Eval Test Project", null);
        await createProject.HandleAsync(projectCmd);

        var createTask = services.GetRequiredService<ICommandHandler<CreateTaskCommand>>();
        var taskCmd = new CreateTaskCommand(projectCmd.CreatedId, "Evaluation Task", null,
            Criticality.High, new float[1024]);
        await createTask.HandleAsync(taskCmd);

        return (projectCmd.CreatedId, taskCmd.CreatedId);
    }
}
