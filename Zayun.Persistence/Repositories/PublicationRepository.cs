using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IPublicationRepository"/>. Links come with it (same row).</summary>
internal sealed class PublicationRepository(ZayunDbContext context)
    : ContentRepository<Publication>(context), IPublicationRepository;
