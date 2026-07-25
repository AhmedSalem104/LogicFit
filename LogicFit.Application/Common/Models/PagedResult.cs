namespace LogicFit.Application.Common.Models;

/// <summary>
/// One-based, bounded pagination result for application-level queries.
/// Its JSON shape is the contract consumed by the platform administration console.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<T>(items, totalCount, page, pageSize, totalPages);
    }
}

public static class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, MaximumPageSize));
}
