namespace SFManagement.Application.DTOs;

public record WorkerDto(
    int Id,
    string Name,
    float[] SkillsVector);
