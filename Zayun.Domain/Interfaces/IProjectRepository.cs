using Zayun.Domain.Entities;
using Zayun.Domain.Enums;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves projects (with their pictures). Implemented in Persistence.</summary>
public interface IProjectRepository : IContentRepository<Project>
{
    /// <summary>The list in display order, optionally one category only. Read-only, pictures included.</summary>
    /// <param name="category">Only this category; null = both.</param>
    /// <param name="publishedOnly">True for the website; false for the admin area.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<IReadOnlyList<Project>> GetListAsync(ProjectCategory? category, bool publishedOnly,
                                              CancellationToken cancellationToken = default);

    /// <summary>One project by its web address. Read-only, pictures included.</summary>
    /// <param name="slug">The web address.</param>
    /// <param name="publishedOnly">True for the website (a draft counts as not found).</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The project, or null.</returns>
    Task<Project?> GetBySlugAsync(string slug, bool publishedOnly, CancellationToken cancellationToken = default);

    /// <summary>True when another project already uses this web address.</summary>
    /// <param name="slug">The web address.</param>
    /// <param name="exceptProjectId">The project being edited (it may keep its own address); null for a new one.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<bool> SlugExistsAsync(string slug, Guid? exceptProjectId, CancellationToken cancellationToken = default);
}
