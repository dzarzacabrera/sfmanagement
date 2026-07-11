namespace SFManagement.Application.DTOs;

public record WorkerScoreDto(
    long Id,
    string Name,
    string Role,
    double CompatibilityScore,
    float[] SkillsVector)
{
    public string IdEncrypted { get; init; } = "";
}
