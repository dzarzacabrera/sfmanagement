using SFManagement.Application.DTOs;

namespace SFManagement.Web.ViewModels;

public record WorkerDetailPopupViewModel(
    WorkerDto Worker,
    IReadOnlyList<SkillDto> AllSkills)
{
    public string WorkerIdEncrypted { get; init; } = "";
}
