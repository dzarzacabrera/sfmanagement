namespace SFManagement.Application.Commands;

public record AddWorkersToProjectCommand(long ProjectId, List<long> WorkerIds);
