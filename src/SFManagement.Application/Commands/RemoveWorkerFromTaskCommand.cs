namespace SFManagement.Application.Commands;

public record RemoveWorkerFromTaskCommand(long TaskId, long WorkerId);
