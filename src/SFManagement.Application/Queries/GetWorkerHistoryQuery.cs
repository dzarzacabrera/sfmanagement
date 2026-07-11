using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetWorkerHistoryQuery(long WorkerId);

public interface IGetWorkerHistoryQueryHandler
{
    Task<IReadOnlyList<EvaluationHistoryDto>> HandleAsync(GetWorkerHistoryQuery query);
}
