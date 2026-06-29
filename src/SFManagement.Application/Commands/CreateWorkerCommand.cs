namespace SFManagement.Application.Commands;

public record CreateWorkerCommand(string Name, string Role, float[] SkillsVector)
{
    public int CreatedId { get; set; }
}
