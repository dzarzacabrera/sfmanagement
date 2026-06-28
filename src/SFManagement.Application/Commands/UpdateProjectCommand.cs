namespace SFManagement.Application.Commands;

public record UpdateProjectCommand(int ProjectId, string Name, string? DescriptionMd);
