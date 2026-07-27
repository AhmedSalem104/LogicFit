using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.Common;

/// <summary>
/// Stable page contract used by every collection endpoint consumed by the platform console.
/// Page numbers are one based and the requested size is clamped to keep platform queries bounded.
/// </summary>
public sealed record PlatformPage<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages)
{
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public static class PlatformPaging
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public static async Task<PlatformPage<T>> CreateAsync<T>(
        IQueryable<T> source,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Normalize(page, pageSize);
        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Create(items, totalCount, page, pageSize);
    }

    public static PlatformPage<T> Create<T>(IReadOnlyCollection<T> source, int page, int pageSize)
    {
        (page, pageSize) = Normalize(page, pageSize);
        var totalCount = source.Count;
        var items = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Create(items, totalCount, page, pageSize);
    }

    private static PlatformPage<T> Create<T>(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PlatformPage<T>(items, totalCount, page, pageSize, totalPages);
    }

    private static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, MaximumPageSize));
}
