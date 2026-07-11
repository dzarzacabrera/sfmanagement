namespace SFManagement.Application.Commands;

public record UpdateProjectCommand(long ProjectId, string Name, string? DescriptionMd);
