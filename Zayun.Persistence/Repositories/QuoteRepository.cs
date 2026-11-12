using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IQuoteRepository"/>.</summary>
internal sealed class QuoteRepository(ZayunDbContext context)
    : ContentRepository<Quote>(context), IQuoteRepository;
