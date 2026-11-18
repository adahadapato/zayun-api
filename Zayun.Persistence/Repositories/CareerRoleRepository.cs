using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ICareerRoleRepository"/>.</summary>
internal sealed class CareerRoleRepository(ZayunDbContext context)
    : ContentRepository<CareerRole>(context), ICareerRoleRepository;
