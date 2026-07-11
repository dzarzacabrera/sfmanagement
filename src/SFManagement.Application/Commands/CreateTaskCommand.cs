using SFManagement.Domain.Enums;

namespace SFManagement.Application.Commands;

public record CreateTaskCommand(
    long ProjectId,
    string Title,
    string? Description,
    Criticality Criticality,
    float[] RequiredSkillsVector)
{
    public long CreatedId { get; set; }
}
