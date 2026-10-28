using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ISkillGroupRepository"/>.</summary>
internal sealed class SkillGroupRepository(ZayunDbContext context)
    : ContentRepository<SkillGroup>(context), ISkillGroupRepository;
