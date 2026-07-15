namespace SFManagement.Application.DTOs;

public record ProjectDto(long Id, string Name, string? DescriptionMd,
    IReadOnlyList<string>? WorkerNames = null, bool IsFinalized = false)
{
    public string IdEncrypted { get; init; } = "";
    public string Status => IsFinalized ? "Closed" : "In Progress";
}
