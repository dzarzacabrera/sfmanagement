using SFManagement.Domain.Enums;

namespace SFManagement.Application.Commands;

public record CreateTaskCommand(
    int ProjectId,
    string Title,
    string? Description,
    Criticality Criticality,
    float[] RequiredSkillsVector)
{
    public int CreatedId { get; set; }
}
