using SFManagement.Domain.ValueObjects;

namespace SFManagement.Domain.Entities;

public class Worker
{
    public long Id { get; init; }
    public string Name { get; private set; }
    public SkillVector SkillsVector { get; private set; }

    public Worker(long id, string name, SkillVector skillsVector)
    {
        Id = id;
        Name = name;
        SkillsVector = skillsVector;
    }

    public void UpdateName(string name) => Name = name;

    public void RecalculateSkills(SkillVector updated) => SkillsVector = updated;
}
