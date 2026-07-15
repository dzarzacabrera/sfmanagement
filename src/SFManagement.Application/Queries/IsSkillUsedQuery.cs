namespace SFManagement.Application.Queries;

public record IsSkillUsedQuery(int VectorPosition);

public interface IIsSkillUsedQueryHandler
{
    Task<bool> HandleAsync(IsSkillUsedQuery query);
    Task<HashSet<int>> GetUsedPositionsAsync();
}
