namespace HappyPaws.Core.Common;

/// <summary>
/// Carries the page number and page size for paginated list queries.
/// </summary>
public record PaginationQuery(int Page = 1, int PageSize = 10);
