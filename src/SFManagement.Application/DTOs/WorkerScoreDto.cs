namespace SFManagement.Application.DTOs;

public record WorkerScoreDto(
    int Id,
    string Name,
    string Role,
    double CompatibilityScore,
    float[] SkillsVector)
{
    public string IdEncrypted { get; init; } = "";
}
