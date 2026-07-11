namespace SFManagement.Application.DTOs;

public record SkillDto(long Id, string Name, int VectorPosition, bool IsActive)
{
    public string IdEncrypted { get; init; } = "";
}
