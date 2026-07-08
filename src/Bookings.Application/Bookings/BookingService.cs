using Bookings.Application.Bookings.Dtos;
using Bookings.Application.Common.Exceptions;
using Bookings.Application.Common.Interfaces;
using Bookings.Application.Common.Results;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Application.Bookings;

public class BookingService : IBookingService
{
    /// <summary>Upper bound on how long a single booking may last.</summary>
    public static readonly TimeSpan MaxBookingDuration = TimeSpan.FromHours(24);

    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public BookingService(IApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<BookingResponse>> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        // --- Domain rule validation (cheap, no DB access) ---
        if (request.ResourceId == Guid.Empty)
        {
            return Error.Validation("A resource id is required.");
        }

        if (request.UserId == Guid.Empty)
        {
            return Error.Validation("A user id is required.");
        }

        if (request.EndsAt <= request.StartsAt)
        {
            return Error.Validation("The booking end time must be after its start time.");
        }

        if (request.EndsAt - request.StartsAt > MaxBookingDuration)
        {
            return Error.Validation($"A booking may not exceed {MaxBookingDuration.TotalHours:0} hours.");
        }

        if (request.StartsAt < now)
        {
            return Error.Validation("A booking cannot start in the past.");
        }

        // --- Referenced entities must exist (and the resource must be bookable) ---
        var resource = await _dbContext.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResourceId, cancellationToken);

        if (resource is null)
        {
            return Error.NotFound($"Resource '{request.ResourceId}' was not found.");
        }

        if (!resource.IsActive)
        {
            return Error.Validation("The resource is not active and cannot be booked.");
        }

        var userExists = await _dbContext.Users
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);

        if (!userExists)
        {
            return Error.NotFound($"User '{request.UserId}' was not found.");
        }

        // --- Overlap check (friendly path) ---
        // Two half-open intervals [s1, e1) and [s2, e2) overlap iff s1 < e2 && s2 < e1.
        // This single predicate covers every case: starts-during, ends-during,
        // contained, containing, and exact match. Cancelled bookings free the slot.
        if (await HasOverlapAsync(request.ResourceId, request.StartsAt, request.EndsAt, cancellationToken))
        {
            return Error.Conflict("The resource is already booked for an overlapping time range.");
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ResourceId = request.ResourceId,
            UserId = request.UserId,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Status = BookingStatus.Confirmed,
            Notes = request.Notes,
            CreatedAt = now
        };

        _dbContext.Bookings.Add(booking);

        try
        {
            // The database exclusion constraint is the definitive guard: if two
            // requests race past the check above, exactly one insert succeeds and
            // the other surfaces here as a conflict.
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (ConflictException)
        {
            return Error.Conflict("The resource is already booked for an overlapping time range.");
        }

        return Result<BookingResponse>.Success(booking.ToResponse());
    }

    public async Task<BookingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _dbContext.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        return booking?.ToResponse();
    }

    public async Task<Result<BookingResponse>> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _dbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (booking is null)
        {
            return Error.NotFound($"Booking '{id}' was not found.");
        }

        // Idempotent: cancelling an already-cancelled booking is a no-op success.
        if (booking.Status == BookingStatus.Cancelled)
        {
            return Result<BookingResponse>.Success(booking.ToResponse());
        }

        if (booking.EndsAt <= _timeProvider.GetUtcNow())
        {
            return Error.Conflict("A booking that has already ended cannot be cancelled.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<BookingResponse>.Success(booking.ToResponse());
    }

    public async Task<Result<IReadOnlyList<BookingResponse>>> GetForResourceAsync(Guid resourceId, BookingQuery query, CancellationToken cancellationToken = default)
    {
        var resourceExists = await _dbContext.Resources
            .AnyAsync(r => r.Id == resourceId, cancellationToken);

        if (!resourceExists)
        {
            return Error.NotFound($"Resource '{resourceId}' was not found.");
        }

        var bookings = await BuildQuery(_dbContext.Bookings.Where(b => b.ResourceId == resourceId), query)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BookingResponse>>.Success(
            bookings.Select(b => b.ToResponse()).ToList());
    }

    public async Task<Result<IReadOnlyList<BookingResponse>>> GetForUserAsync(Guid userId, BookingQuery query, CancellationToken cancellationToken = default)
    {
        var userExists = await _dbContext.Users
            .AnyAsync(u => u.Id == userId, cancellationToken);

        if (!userExists)
        {
            return Error.NotFound($"User '{userId}' was not found.");
        }

        var bookings = await BuildQuery(_dbContext.Bookings.Where(b => b.UserId == userId), query)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BookingResponse>>.Success(
            bookings.Select(b => b.ToResponse()).ToList());
    }

    private Task<bool> HasOverlapAsync(Guid resourceId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken)
    {
        return _dbContext.Bookings.AnyAsync(b =>
            b.ResourceId == resourceId &&
            b.Status != BookingStatus.Cancelled &&
            b.StartsAt < endsAt &&
            startsAt < b.EndsAt,
            cancellationToken);
    }

    private static IQueryable<Booking> BuildQuery(IQueryable<Booking> source, BookingQuery query)
    {
        source = source.AsNoTracking();

        if (!query.IncludeCancelled)
        {
            source = source.Where(b => b.Status != BookingStatus.Cancelled);
        }

        // Restrict to bookings overlapping the [From, To) window when provided.
        if (query.From is { } from)
        {
            source = source.Where(b => b.EndsAt > from);
        }

        if (query.To is { } to)
        {
            source = source.Where(b => b.StartsAt < to);
        }

        return source.OrderBy(b => b.StartsAt);
    }
}
