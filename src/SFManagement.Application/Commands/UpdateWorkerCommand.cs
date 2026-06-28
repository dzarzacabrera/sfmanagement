namespace SFManagement.Application.Commands;

public record UpdateWorkerCommand(int WorkerId, string Name, string Role, float[]? SkillsVector = null);

