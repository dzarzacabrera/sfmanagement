using SFManagement.Application.DTOs;
using SFManagement.Domain.Enums;

namespace SFManagement.Application.Queries;

public record GetWorkerTasksQuery(long WorkerId);

public record WorkerTaskDto(long TaskId, string TaskTitle, string ProjectName, long ProjectId, ProjectTaskStatus Status, Criticality Criticality)
{
    public string TaskIdEncrypted { get; init; } = "";
    public string ProjectIdEncrypted { get; init; } = "";
}

public interface IGetWorkerTasksQueryHandler
{
    Task<IReadOnlyList<WorkerTaskDto>> HandleAsync(GetWorkerTasksQuery query);
}
