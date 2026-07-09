using Bookings.Application.Bookings.Dtos;
using Bookings.Application.Common.Pagination;
using Bookings.Application.Common.Results;

namespace Bookings.Application.Bookings;

/// <summary>Query filter and pagination for listing bookings.</summary>
public record BookingQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    bool IncludeCancelled = false,
    int Page = 1,
    int PageSize = 20);

public interface IBookingService
{
    /// <summary>
    /// Creates a booking owned by <paramref name="userId"/> after validating
    /// domain rules and checking for overlaps. Conflict if the slot is taken.
    /// </summary>
    Task<Result<BookingResponse>> CreateAsync(Guid userId, CreateBookingRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets a booking. Forbidden if it is not owned by <paramref name="userId"/>.</summary>
    Task<Result<BookingResponse>> GetByIdAsync(Guid userId, Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a booking, freeing its slot. Forbidden if it is not owned by
    /// <paramref name="userId"/>. Idempotent for an already-cancelled booking.
    /// </summary>
    Task<Result<BookingResponse>> CancelAsync(Guid userId, Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>Lists bookings for a resource. Not-found if the resource does not exist.</summary>
    Task<Result<PagedResult<BookingResponse>>> GetForResourceAsync(Guid resourceId, BookingQuery query, CancellationToken cancellationToken = default);

    /// <summary>Lists the bookings owned by <paramref name="userId"/>.</summary>
    Task<PagedResult<BookingResponse>> GetForUserAsync(Guid userId, BookingQuery query, CancellationToken cancellationToken = default);
}
