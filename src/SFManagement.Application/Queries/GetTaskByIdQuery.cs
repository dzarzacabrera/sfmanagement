using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetTaskByIdQuery(long TaskId);

public interface IGetTaskByIdQueryHandler
{
    Task<TaskDto?> HandleAsync(GetTaskByIdQuery query);
}
