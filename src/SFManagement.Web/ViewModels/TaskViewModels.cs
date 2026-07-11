using SFManagement.Application.DTOs;
using SFManagement.Domain.Enums;

namespace SFManagement.Web.ViewModels;

public record CreateProjectViewModel();

public record ProjectDetailViewModel(
    long Id,
    string Name,
    string? DescriptionMd,
    IReadOnlyList<WorkerDto>? Workers = null,
    bool HasWorkersToAssignToProject = true)
{
    public string IdEncrypted { get; init; } = "";
}

public record CreateTaskViewModel(
    long ProjectId,
    IReadOnlyList<ProjectDto> Projects,
    IReadOnlyList<SkillCatalogueItem> Skills,
    IReadOnlyList<CriticalityOption> Criticalities)
{
    public string ProjectIdEncrypted { get; init; } = "";
}

public record EditTaskViewModel(
    long TaskId,
    long ProjectId,
    string Title,
    string? Description,
    Criticality Criticality,
    IReadOnlyList<ProjectDto> Projects,
    IReadOnlyList<SkillCatalogueItem> Skills,
    IReadOnlyList<CriticalityOption> Criticalities)
{
    public string TaskIdEncrypted { get; init; } = "";
    public string ProjectIdEncrypted { get; init; } = "";
}

public record SkillCatalogueItem(long Id, string Name, int Position)
{
    public string IdEncrypted { get; init; } = "";
}

public record CriticalityOption(Criticality Value, string Label);
