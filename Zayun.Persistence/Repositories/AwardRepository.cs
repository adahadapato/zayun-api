using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IAwardRepository"/>.</summary>
internal sealed class AwardRepository(ZayunDbContext context)
    : ContentRepository<Award>(context), IAwardRepository;
