namespace SFManagement.Domain.Entities;

public class SkillCatalogue
{
    public int Id { get; init; }
    public string Name { get; private set; }
    public int VectorPosition { get; init; }

    public SkillCatalogue(int id, string name, int vectorPosition)
    {
        Id = id;
        Name = name;
        VectorPosition = vectorPosition;
    }

    public void UpdateName(string name) => Name = name;
}
