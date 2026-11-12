using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves quotes for the "Quote of the day" card. Implemented in Persistence.</summary>
public interface IQuoteRepository : IContentRepository<Quote>;
