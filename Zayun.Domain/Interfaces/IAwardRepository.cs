using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves awards. Implemented in Persistence.</summary>
public interface IAwardRepository : IContentRepository<Award>;
