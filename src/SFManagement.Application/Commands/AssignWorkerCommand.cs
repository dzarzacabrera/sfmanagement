namespace SFManagement.Application.Commands;

public record AssignWorkerCommand(long TaskId, long WorkerId);
