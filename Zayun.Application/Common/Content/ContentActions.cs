using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Errors;
using Zayun.Domain.Shared.Results;

namespace Zayun.Application.Common.Content;

/// <summary>
/// The three admin actions every kind of website content shares: publish / unpublish, delete, and reorder. Each
/// feature keeps its own small command (with its own cache tag) and calls these.
/// </summary>
public static class ContentActions
{
    /// <summary>Shows or hides one item on the website.</summary>
    /// <typeparam name="T">The content type.</typeparam>
    /// <param name="repository">Its repository.</param>
    /// <param name="unitOfWork">Saves the change.</param>
    /// <param name="id">The item.</param>
    /// <param name="isPublished">True to show it, false to hide it.</param>
    /// <param name="notFound">The error when there's no such item.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>Success, or <paramref name="notFound"/>.</returns>
    public static async Task<Result> SetPublishedAsync<T>(IContentRepository<T> repository, IUnitOfWork unitOfWork,
                                                          Guid id, bool isPublished, Error notFound,
                                                          CancellationToken ct)
        where T : ContentEntity
    {
        var item = await repository.GetByIdAsync(id, ct);
        if (item is null) return notFound;

        if (isPublished) item.Publish();
        else item.Unpublish();

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Deletes one item (soft delete: it disappears from the website and the admin lists).</summary>
    /// <typeparam name="T">The content type.</typeparam>
    /// <param name="repository">Its repository.</param>
    /// <param name="unitOfWork">Saves the change.</param>
    /// <param name="clock">When it was deleted.</param>
    /// <param name="id">The item.</param>
    /// <param name="notFound">The error when there's no such item.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>Success, or <paramref name="notFound"/>.</returns>
    public static async Task<Result> DeleteAsync<T>(IContentRepository<T> repository, IUnitOfWork unitOfWork,
                                                    TimeProvider clock, Guid id, Error notFound, CancellationToken ct)
        where T : ContentEntity
    {
        var item = await repository.GetByIdAsync(id, ct);
        if (item is null) return notFound;

        item.Delete(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Drag-to-reorder: gives every item its new position.</summary>
    /// <typeparam name="T">The content type.</typeparam>
    /// <param name="repository">Its repository.</param>
    /// <param name="unitOfWork">Saves the change.</param>
    /// <param name="idsInOrder">Every item's Id (drafts included), each once, in the new order.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>Success, or ContentErrors.ReorderMismatch when the list doesn't match what's in the database
    /// (e.g. someone added an item in another tab).</returns>
    public static async Task<Result> ReorderAsync<T>(IContentRepository<T> repository, IUnitOfWork unitOfWork,
                                                     IReadOnlyList<Guid> idsInOrder, CancellationToken ct)
        where T : ContentEntity
    {
        var all = await repository.GetAllForUpdateAsync(ct);
        if (all.Count != idsInOrder.Count || !all.Select(i => i.Id).ToHashSet().SetEquals(idsInOrder))
            return ContentErrors.ReorderMismatch;

        var byId = all.ToDictionary(i => i.Id);
        for (var i = 0; i < idsInOrder.Count; i++)
            byId[idsInOrder[i]].MoveTo(i);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
