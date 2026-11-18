using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves career roles (the "Roles" on the Experience page). Implemented in Persistence.</summary>
public interface ICareerRoleRepository : IContentRepository<CareerRole>;
