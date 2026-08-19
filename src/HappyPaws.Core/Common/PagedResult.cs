namespace HappyPaws.Core.Common;

/// <summary>
/// Wraps a page of items alongside the metadata needed to drive pagination controls.
/// </summary>
public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize)
{
    /// <summary>Total number of pages, rounded up.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>True when the current page is not the last.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>True when the current page is not the first.</summary>
    public bool HasPreviousPage => Page > 1;
}
