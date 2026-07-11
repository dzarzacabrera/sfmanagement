namespace SFManagement.Application.Commands;

public record AddWorkerToProjectCommand(long ProjectId, long WorkerId);
