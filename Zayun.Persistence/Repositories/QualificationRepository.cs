using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;

namespace Zayun.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IQualificationRepository"/>.</summary>
internal sealed class QualificationRepository(ZayunDbContext context)
    : ContentRepository<Qualification>(context), IQualificationRepository;
