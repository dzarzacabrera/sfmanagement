namespace SFManagement.Application.DTOs;

public record WorkerScoreDto(
    long Id,
    string Name,
    string Role,
    double CompatibilityScore,
    float[] SkillsVector,
    bool IsAssigned = false)
{
    public string IdEncrypted { get; init; } = "";
}
