using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetDashboardTasksQuery(int ProjectId);

public interface IGetDashboardTasksQueryHandler
{
    Task<IReadOnlyList<TaskDto>> HandleAsync(GetDashboardTasksQuery query);
}
