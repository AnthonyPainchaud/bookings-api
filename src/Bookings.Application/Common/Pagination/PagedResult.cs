namespace Bookings.Application.Common.Pagination;

/// <summary>
/// A page of results plus the metadata a client needs to page through the rest
/// (current page, page size, total item count, and derived total page count).
/// </summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
