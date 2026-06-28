namespace SFManagement.Application.Commands;

public record RemoveWorkerFromTaskCommand(int TaskId, int WorkerId);
