using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetAllWorkersQuery();

public interface IGetAllWorkersQueryHandler
{
    Task<IReadOnlyList<WorkerDto>> HandleAsync(GetAllWorkersQuery query);
}
