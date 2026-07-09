using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetTaskByIdQuery(int TaskId);

public interface IGetTaskByIdQueryHandler
{
    Task<TaskDto?> HandleAsync(GetTaskByIdQuery query);
}
