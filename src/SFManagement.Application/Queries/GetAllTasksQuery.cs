using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetAllTasksQuery();

public interface IGetAllTasksQueryHandler
{
    Task<IReadOnlyList<TaskDto>> HandleAsync(GetAllTasksQuery query);
}
