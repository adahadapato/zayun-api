using Zayun.Domain.Entities;
using Zayun.Domain.Enums;

namespace Zayun.Domain.Interfaces;

/// <summary>Loads and saves private documents. Implemented in Persistence.</summary>
public interface IDocumentRepository : IRepository<Document>
{
    /// <summary>The documents, newest first, optionally one category. Read-only.</summary>
    /// <param name="category">Only this category; null = all.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<IReadOnlyList<Document>> GetListAsync(DocumentCategory? category, CancellationToken cancellationToken = default);

    /// <summary>How many documents there are.</summary>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
