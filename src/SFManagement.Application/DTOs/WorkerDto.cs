namespace SFManagement.Application.DTOs;

public record WorkerDto(
    long Id,
    string Name,
    string Role,
    int EvaluationCount,
    int ActiveTaskCount,
    float[] SkillsVector)
{
    public string IdEncrypted { get; init; } = "";
}

