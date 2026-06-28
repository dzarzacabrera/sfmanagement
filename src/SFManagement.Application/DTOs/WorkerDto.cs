namespace SFManagement.Application.DTOs;

public record WorkerDto(
    int Id,
    string Name,
    string Role,
    int EvaluationCount,
    float[] SkillsVector);

