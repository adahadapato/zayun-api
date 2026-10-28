using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves skill groups. Implemented in Persistence.</summary>
public interface ISkillGroupRepository : IContentRepository<SkillGroup>;
