using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IPresentationRepository"/>.</summary>
internal sealed class PresentationRepository(ZayunDbContext context)
    : ContentRepository<Presentation>(context), IPresentationRepository;
