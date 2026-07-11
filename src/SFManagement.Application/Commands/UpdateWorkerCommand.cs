namespace SFManagement.Application.Commands;

public record UpdateWorkerCommand(long WorkerId, string Name, string Role, float[]? SkillsVector = null);

