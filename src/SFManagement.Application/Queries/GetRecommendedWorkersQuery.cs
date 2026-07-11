using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetRecommendedWorkersQuery(long ProjectId, long TaskId);

public interface IGetRecommendedWorkersQueryHandler
{
    Task<IReadOnlyList<WorkerScoreDto>> HandleAsync(GetRecommendedWorkersQuery query);
}
