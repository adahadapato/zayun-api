using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves publications (with their links). Implemented in Persistence.</summary>
public interface IPublicationRepository : IContentRepository<Publication>;
