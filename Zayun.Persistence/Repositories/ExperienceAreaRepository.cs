using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IExperienceAreaRepository"/>.</summary>
internal sealed class ExperienceAreaRepository(ZayunDbContext context)
    : ContentRepository<ExperienceArea>(context), IExperienceAreaRepository;
