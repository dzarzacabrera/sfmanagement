using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetAllSkillsQuery(bool IncludeInactive = false);

public interface IGetAllSkillsQueryHandler
{
    Task<List<SkillDto>> HandleAsync(GetAllSkillsQuery query);
}
