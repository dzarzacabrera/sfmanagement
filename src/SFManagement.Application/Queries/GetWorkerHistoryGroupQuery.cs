using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetWorkerHistoryGroupQuery(long WorkerId);

public interface IGetWorkerHistoryGroupQueryHandler
{
    Task<IReadOnlyList<EvaluationHistoryGroupDto>> HandleAsync(GetWorkerHistoryGroupQuery query);
}
