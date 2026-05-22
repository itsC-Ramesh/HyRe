using Microsoft.EntityFrameworkCore;

namespace RC.HyRe.Application.Common.Models;

/// <summary>
/// A page of results with metadata. Created via the static factory method
/// to keep async EF materialisation out of repository constructors.
/// </summary>
public class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int Limit { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Limit);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    private PaginatedList(IReadOnlyList<T> items, int totalCount, int page, int limit)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        Limit = limit;
    }

    /// <summary>
    /// Applies pagination to an IQueryable and materialises the result.
    /// The count query and data query share the same filter expression,
    /// so only two DB round-trips are made.
    /// </summary>
    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, totalCount, page, limit);
    }

    /// <summary>
    /// Creates a PaginatedList from an already-materialised in-memory list.
    /// Use when the data has been projected or fetched in a separate query.
    /// </summary>
    public static PaginatedList<T> Create(IReadOnlyList<T> items, int totalCount, int page, int limit)
    {
        return new PaginatedList<T>(items, totalCount, page, limit);
    }
}
