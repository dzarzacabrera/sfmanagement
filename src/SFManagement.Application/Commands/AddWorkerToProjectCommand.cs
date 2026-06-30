namespace SFManagement.Application.Commands;

public record AddWorkerToProjectCommand(int ProjectId, int WorkerId);
