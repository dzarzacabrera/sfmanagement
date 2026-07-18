using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;

namespace SFManagement.IntegrationTests;

[Collection("Database")]
public sealed class WorkerAssignmentTests(SfManagementFixture fixture)
{
    [Fact]
    public async Task AssignWorkerToTask_ShouldSucceed()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<AssignWorkerCommand>>();

        var command = new AssignWorkerCommand(1, 1);
        await handler.HandleAsync(command);
    }

    [Fact]
    public async Task GetRecommendedWorkers_ShouldReturnOrderedByCompatibility()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IGetRecommendedWorkersQueryHandler>();

        var result = await handler.HandleAsync(new GetRecommendedWorkersQuery(1, 1));

        result.Should().NotBeEmpty();
        result.Should().BeInDescendingOrder(w => w.CompatibilityScore);
        result.All(w => w.CompatibilityScore >= 0).Should().BeTrue();
    }

    [Fact]
    public async Task GetWorkersByProject_ShouldReturnOnlyProjectWorkers()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IGetWorkersByProjectQueryHandler>();

        var project1Workers = await handler.HandleAsync(new GetWorkersByProjectQuery(1));
        var project2Workers = await handler.HandleAsync(new GetWorkersByProjectQuery(2));

        project1Workers.Should().HaveCount(8);
        project2Workers.Should().HaveCount(6);
    }
}
