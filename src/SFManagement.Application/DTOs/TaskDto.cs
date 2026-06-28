using SFManagement.Domain.Enums;

namespace SFManagement.Application.DTOs;

public record TaskDto(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    Criticality Criticality,
    ProjectTaskStatus Status,
    float[] RequiredSkillsVector,
    int? AssignedWorkerId,
    string? AssignedWorkerName);
