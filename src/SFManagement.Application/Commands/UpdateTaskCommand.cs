using SFManagement.Domain.Enums;

namespace SFManagement.Application.Commands;

public record UpdateTaskCommand(
    long TaskId,
    long ProjectId,
    string Title,
    string? Description,
    Criticality Criticality,
    float[] RequiredSkillsVector);
