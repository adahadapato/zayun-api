using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves presentations. Implemented in Persistence.</summary>
public interface IPresentationRepository : IContentRepository<Presentation>;
