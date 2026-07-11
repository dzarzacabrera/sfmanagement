namespace SFManagement.Application.DTOs;

public record ProjectDto(long Id, string Name, string? DescriptionMd)
{
    public string IdEncrypted { get; init; } = "";
}
