using SFManagement.Domain.Enums;

namespace SFManagement.Web.ViewModels;

public record CreateProjectViewModel();

public record ProjectDetailViewModel(int Id, string Name, string? DescriptionMd);

public record CreateTaskViewModel(
    int ProjectId,
    IReadOnlyList<SkillCatalogueItem> Skills,
    IReadOnlyList<CriticalityOption> Criticalities);

public record SkillCatalogueItem(int Id, string Name, int Position);

public record CriticalityOption(Criticality Value, string Label);
