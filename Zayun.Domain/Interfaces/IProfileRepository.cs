using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves online profiles. Implemented in Persistence.</summary>
public interface IProfileRepository : IContentRepository<Profile>;
