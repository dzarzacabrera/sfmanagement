namespace SFManagement.Application.Commands;

public record UpdateSkillCatalogueCommand(long SkillId, string Name, string Description);
