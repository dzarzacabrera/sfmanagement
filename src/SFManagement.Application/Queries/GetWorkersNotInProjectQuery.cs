using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetWorkersNotInProjectQuery(long ProjectId);

public interface IGetWorkersNotInProjectQueryHandler
{
    Task<IReadOnlyList<WorkerDto>> HandleAsync(GetWorkersNotInProjectQuery query);
}
