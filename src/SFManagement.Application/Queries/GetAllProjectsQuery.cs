using SFManagement.Application.DTOs;

namespace SFManagement.Application.Queries;

public record GetAllProjectsQuery();

public interface IGetAllProjectsQueryHandler
{
    Task<IReadOnlyList<ProjectDto>> HandleAsync(GetAllProjectsQuery query);
}
