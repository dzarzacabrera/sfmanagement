namespace SFManagement.Domain.Entities;

public class Project
{
    public int Id { get; init; }
    public string Name { get; private set; }
    public string? DescriptionMd { get; private set; }

    public Project(int id, string name, string? descriptionMd)
    {
        Id = id;
        Name = name;
        DescriptionMd = descriptionMd;
    }

    public void UpdateDetails(string name, string? descriptionMd)
    {
        Name = name;
        DescriptionMd = descriptionMd;
    }
}
