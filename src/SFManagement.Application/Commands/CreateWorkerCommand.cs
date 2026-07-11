namespace SFManagement.Application.Commands;

public record CreateWorkerCommand(string Name, string Role, float[] SkillsVector)
{
    public long CreatedId { get; set; }
}
