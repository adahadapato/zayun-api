using Zayun.Domain.Entities;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves qualifications (the "Education" on the Experience page). Implemented in Persistence.</summary>
public interface IQualificationRepository : IContentRepository<Qualification>;
