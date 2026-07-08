using Bookings.Application.Bookings.Dtos;
using Bookings.Application.Common.Results;

namespace Bookings.Application.Bookings;

/// <summary>Query filter for listing bookings.</summary>
public record BookingQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    bool IncludeCancelled = false);

public interface IBookingService
{
    /// <summary>
    /// Creates a booking after validating domain rules and checking for overlaps.
    /// Fails with a conflict if the resource is already booked for the range.
    /// </summary>
    Task<Result<BookingResponse>> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default);

    Task<BookingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Cancels a booking. Idempotent for an already-cancelled booking.</summary>
    Task<Result<BookingResponse>> CancelAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lists bookings for a resource. Fails with not-found if the resource does not exist.</summary>
    Task<Result<IReadOnlyList<BookingResponse>>> GetForResourceAsync(Guid resourceId, BookingQuery query, CancellationToken cancellationToken = default);

    /// <summary>Lists bookings for a user. Fails with not-found if the user does not exist.</summary>
    Task<Result<IReadOnlyList<BookingResponse>>> GetForUserAsync(Guid userId, BookingQuery query, CancellationToken cancellationToken = default);
}
