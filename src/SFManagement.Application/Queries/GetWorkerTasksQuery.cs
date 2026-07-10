using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetWorkerTasksQuery(int WorkerId);

public record WorkerTaskDto(int TaskId, string TaskTitle, string ProjectName, int ProjectId)
{
    public string TaskIdEncrypted { get; init; } = "";
    public string ProjectIdEncrypted { get; init; } = "";
}

public interface IGetWorkerTasksQueryHandler
{
    Task<IReadOnlyList<WorkerTaskDto>> HandleAsync(GetWorkerTasksQuery query);
}
