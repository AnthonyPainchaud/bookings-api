using System.ComponentModel.DataAnnotations;

namespace Bookings.Api.Common;

/// <summary>Common paging query parameters shared by every list endpoint.</summary>
public class PaginationParameters
{
    private const int MaxPageSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "page must be at least 1.")]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "pageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;
}
