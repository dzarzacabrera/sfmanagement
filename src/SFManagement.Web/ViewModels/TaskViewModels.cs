using SFManagement.Application.DTOs;
using SFManagement.Domain.Enums;

namespace SFManagement.Web.ViewModels;

public record CreateProjectViewModel();

public record ProjectDetailViewModel(
    int Id,
    string Name,
    string? DescriptionMd,
    IReadOnlyList<WorkerDto>? Workers = null);

public record CreateTaskViewModel(
    int ProjectId,
    IReadOnlyList<ProjectDto> Projects,
    IReadOnlyList<SkillCatalogueItem> Skills,
    IReadOnlyList<CriticalityOption> Criticalities);

public record EditTaskViewModel(
    int TaskId,
    int ProjectId,
    string Title,
    string? Description,
    Criticality Criticality,
    IReadOnlyList<ProjectDto> Projects,
    IReadOnlyList<SkillCatalogueItem> Skills,
    IReadOnlyList<CriticalityOption> Criticalities);

public record SkillCatalogueItem(int Id, string Name, int Position);

public record CriticalityOption(Criticality Value, string Label);
