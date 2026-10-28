namespace Zayun.Presentation.Endpoints.Shared;

/// <summary>Body of every "show or hide on the website" route.</summary>
/// <param name="IsPublished">True to show, false to hide.</param>
public sealed record PublishedRequest(bool IsPublished);

/// <summary>Body of every drag-to-reorder route.</summary>
/// <param name="Ids">Every item's Id, each once, in the new order.</param>
public sealed record OrderRequest(IReadOnlyList<Guid> Ids);

/// <summary>Body for editing an experience area or a skill group (the Id is in the route).</summary>
/// <param name="Title">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="Skills">The skills, in order (replaces the old list).</param>
public sealed record TextGroupRequest(string Title, string Description, IReadOnlyList<string>? Skills);

/// <summary>Body for renaming a core expertise item.</summary>
/// <param name="Name">The new name.</param>
public sealed record NameRequest(string Name);
