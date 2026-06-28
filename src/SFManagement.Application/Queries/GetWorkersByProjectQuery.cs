using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetWorkersByProjectQuery(int ProjectId);

public interface IGetWorkersByProjectQueryHandler
{
    Task<IReadOnlyList<WorkerDto>> HandleAsync(GetWorkersByProjectQuery query);
}
