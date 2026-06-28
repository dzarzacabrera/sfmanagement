using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Domain.Enums;

namespace SFManagement.IntegrationTests;

[Collection("Database")]
public sealed class TaskStatusTransitionTests(SfManagementFixture fixture)
{
    [Fact]
    public async Task QueuedToInProgressToFinish_ShouldSucceed()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ChangeTaskStatusCommand>>();

        await handler.HandleAsync(new ChangeTaskStatusCommand(1, ProjectTaskStatus.InProgress));
        await handler.HandleAsync(new ChangeTaskStatusCommand(1, ProjectTaskStatus.Finish));
    }

    [Fact]
    public async Task QueuedToFinish_ShouldThrow()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ChangeTaskStatusCommand>>();

        await FluentActions.Invoking(() =>
            handler.HandleAsync(new ChangeTaskStatusCommand(1, ProjectTaskStatus.Finish)))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task FinishToAny_ShouldThrow()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ChangeTaskStatusCommand>>();

        await FluentActions.Invoking(() =>
            handler.HandleAsync(new ChangeTaskStatusCommand(4, ProjectTaskStatus.Queued)))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task BlockedToQueued_ShouldSucceed()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ChangeTaskStatusCommand>>();

        await handler.HandleAsync(new ChangeTaskStatusCommand(3, ProjectTaskStatus.Queued));
    }
}
