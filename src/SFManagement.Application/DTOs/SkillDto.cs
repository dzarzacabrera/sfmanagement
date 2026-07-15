namespace SFManagement.Application.DTOs;

public record SkillDto(long Id, string Name, string Description, int VectorPosition, bool IsActive)
{
    public string IdEncrypted { get; init; } = "";
}
