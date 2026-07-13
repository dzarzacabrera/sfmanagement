namespace SFManagement.Application.Commands;

public record AssignWorkersCommand(long TaskId, IReadOnlyList<long> WorkerIds);
