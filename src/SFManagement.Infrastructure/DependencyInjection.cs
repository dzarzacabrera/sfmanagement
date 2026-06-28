using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SFManagement.Application.Abstractions;
using SFManagement.Application.Commands;
using SFManagement.Application.Queries;
using SFManagement.Infrastructure.Data;
using SFManagement.Infrastructure.Handlers.Commands;
using SFManagement.Infrastructure.Handlers.Queries;

namespace SFManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        var dataSource = dataSourceBuilder.Build();
        services.AddSingleton(dataSource);
        services.AddSingleton<INpgsqlConnectionFactory, NpgsqlConnectionFactory>();

        services.AddTransient<ICommandHandler<CreateProjectCommand>, CreateProjectCommandHandler>();
        services.AddTransient<ICommandHandler<CreateTaskCommand>, CreateTaskCommandHandler>();
        services.AddTransient<ICommandHandler<AssignWorkerCommand>, AssignWorkerCommandHandler>();
        services.AddTransient<ICommandHandler<ChangeTaskStatusCommand>, ChangeTaskStatusCommandHandler>();
        services.AddTransient<ICommandHandler<EvaluateTaskCommand>, EvaluateTaskCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateProjectCommand>, UpdateProjectCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateTaskCommand>, UpdateTaskCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateWorkerCommand>, UpdateWorkerCommandHandler>();
        services.AddTransient<ICommandHandler<UpdateSkillCatalogueCommand>, UpdateSkillCatalogueCommandHandler>();
        services.AddTransient<ICommandHandler<CreateSkillCommand>, CreateSkillCommandHandler>();
        services.AddTransient<ICommandHandler<ToggleSkillActiveCommand>, ToggleSkillActiveCommandHandler>();
        services.AddTransient<ICommandHandler<CreateWorkerCommand>, CreateWorkerCommandHandler>();

        services.AddTransient<IGetDashboardTasksQueryHandler, GetDashboardTasksQueryHandler>();
        services.AddTransient<IGetRecommendedWorkersQueryHandler, GetRecommendedWorkersQueryHandler>();
        services.AddTransient<IGetWorkerHistoryQueryHandler, GetWorkerHistoryQueryHandler>();
        services.AddTransient<IGetWorkersByProjectQueryHandler, GetWorkersByProjectQueryHandler>();
        services.AddTransient<IGetAllSkillsQueryHandler, GetAllSkillsQueryHandler>();
        services.AddTransient<IGetAllWorkersQueryHandler, GetAllWorkersQueryHandler>();
        services.AddTransient<IGetAllProjectsQueryHandler, GetAllProjectsQueryHandler>();
        services.AddTransient<IGetAllTasksQueryHandler, GetAllTasksQueryHandler>();

        return services;
    }
}
