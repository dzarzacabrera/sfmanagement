using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.DTOs;
using SFManagement.Application.Queries;

namespace SFManagement.IntegrationTests;

[Collection("Database")]
public sealed class ProjectLifecycleTests(SfManagementFixture fixture)
{
    [Fact]
    public async Task CreateProject_ShouldInsertAndReturnId()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateProjectCommand>>();

        var command = new CreateProjectCommand("Integration Test Project", "# Test Description");
        await handler.HandleAsync(command);

        command.CreatedId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateProject_ShouldModifyFields()
    {
        using var scope = fixture.Factory.Services.CreateScope();

        var createHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateProjectCommand>>();
        var createCmd = new CreateProjectCommand("Original Name", null);
        await createHandler.HandleAsync(createCmd);

        var updateHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateProjectCommand>>();
        var updateCmd = new UpdateProjectCommand(createCmd.CreatedId, "Updated Name", "Updated desc");
        await updateHandler.HandleAsync(updateCmd);

        var queryHandler = scope.ServiceProvider.GetRequiredService<IGetWorkersByProjectQueryHandler>();
        var workers = await queryHandler.HandleAsync(new GetWorkersByProjectQuery(createCmd.CreatedId));
        workers.Should().NotBeNull();
    }
}
