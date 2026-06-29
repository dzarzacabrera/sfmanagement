namespace SFManagement.Application.DTOs;

public record WorkerDto(
    int Id,
    string Name,
    string Role,
    int EvaluationCount,
    int ActiveTaskCount,
    float[] SkillsVector);

