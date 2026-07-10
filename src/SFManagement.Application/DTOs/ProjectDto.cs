namespace SFManagement.Application.DTOs;

public record ProjectDto(int Id, string Name, string? DescriptionMd)
{
    public string IdEncrypted { get; init; } = "";
}
