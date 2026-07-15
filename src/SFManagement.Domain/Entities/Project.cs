namespace SFManagement.Domain.Entities;

public class Project
{
    public long Id { get; init; }
    public string Name { get; private set; }
    public string? DescriptionMd { get; private set; }
    public bool IsFinalized { get; private set; }

    public Project(long id, string name, string? descriptionMd, bool isFinalized = false)
    {
        Id = id;
        Name = name;
        DescriptionMd = descriptionMd;
        IsFinalized = isFinalized;
    }

    public void UpdateDetails(string name, string? descriptionMd)
    {
        Name = name;
        DescriptionMd = descriptionMd;
    }

    public void MarkAsFinalized()
    {
        IsFinalized = true;
    }
}
