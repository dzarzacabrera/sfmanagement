namespace SFManagement.Application.DTOs;

public record ProjectDto(long Id, string Name, string? DescriptionMd,
    IReadOnlyList<string>? WorkerNames = null)
{
    public string IdEncrypted { get; init; } = "";
}
