namespace SFManagement.Application.Commands;

public record CreateWorkerCommand(string Name)
{
    public int CreatedId { get; set; }
}
