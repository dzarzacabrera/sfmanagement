namespace SFManagement.Domain.Entities;

public class SkillCatalogue
{
    public long Id { get; init; }
    public string Name { get; private set; }
    public int VectorPosition { get; init; }
    public bool IsActive { get; private set; }

    public SkillCatalogue(long id, string name, int vectorPosition, bool isActive = true)
    {
        Id = id;
        Name = name;
        VectorPosition = vectorPosition;
        IsActive = isActive;
    }

    public void UpdateName(string name) => Name = name;

    public void ToggleActive() => IsActive = !IsActive;
}
