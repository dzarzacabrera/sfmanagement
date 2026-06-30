using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetWorkersNotInProjectQuery(int ProjectId);

public interface IGetWorkersNotInProjectQueryHandler
{
    Task<IReadOnlyList<WorkerDto>> HandleAsync(GetWorkersNotInProjectQuery query);
}
