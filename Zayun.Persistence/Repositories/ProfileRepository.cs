using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IProfileRepository"/>.</summary>
internal sealed class ProfileRepository(ZayunDbContext context)
    : ContentRepository<Profile>(context), IProfileRepository;
