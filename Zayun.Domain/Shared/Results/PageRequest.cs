namespace Zayun.Domain.Shared.Results;

/// <summary>
/// What the app asks for: which page, and how many items per page.
/// Bad values are corrected instead of crashing the query.
/// </summary>
public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private readonly int _page = 1;
    private readonly int _pageSize = DefaultPageSize;

    /// <summary>1-based page number. 0 or negative becomes 1.</summary>
    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    /// <summary>Items per page, kept between 1 and MaxPageSize.</summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    /// <summary>How many items to skip: use as .Skip(request.Skip).Take(request.PageSize).</summary>
    public int Skip => (Page - 1) * PageSize;
}