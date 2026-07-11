namespace SFManagement.Application.Commands;

public record CreateProjectCommand(string Name, string? DescriptionMd)
{
    public long CreatedId { get; set; }
}
