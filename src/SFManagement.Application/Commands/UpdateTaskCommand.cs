using SFManagement.Domain.Enums;

namespace SFManagement.Application.Commands;

public record UpdateTaskCommand(
    int TaskId,
    string Title,
    string? Description,
    Criticality Criticality,
    float[] RequiredSkillsVector);
