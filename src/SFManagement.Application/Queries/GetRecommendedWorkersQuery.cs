using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetRecommendedWorkersQuery(int ProjectId, int TaskId);

public interface IGetRecommendedWorkersQueryHandler
{
    Task<IReadOnlyList<WorkerScoreDto>> HandleAsync(GetRecommendedWorkersQuery query);
}
