namespace SFManagement.Application.Commands;

public record CreateProjectCommand(string Name, string? DescriptionMd)
{
    public int CreatedId { get; set; }
}
