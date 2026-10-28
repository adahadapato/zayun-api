using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves experience areas. Implemented in Persistence.</summary>
public interface IExperienceAreaRepository : IContentRepository<ExperienceArea>;
