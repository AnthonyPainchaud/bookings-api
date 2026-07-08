using Bookings.Api.Common;
using Bookings.Application.Bookings;
using Bookings.Application.Bookings.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Api.Controllers;

[Route("api/[controller]")]
public class BookingsController : ApiControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>Creates a booking. Returns 409 if the slot is already taken.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponse>> Create(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookingService.CreateAsync(request, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : HandleError(result.Error!);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _bookingService.GetByIdAsync(id, cancellationToken);
        return booking is null ? NotFound() : Ok(booking);
    }

    /// <summary>Cancels a booking, freeing its time slot. Idempotent.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookingService.CancelAsync(id, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error!);
    }

    [HttpGet("/api/resources/{resourceId:guid}/bookings")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> GetForResource(
        Guid resourceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] bool includeCancelled = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookingService.GetForResourceAsync(
            resourceId, new BookingQuery(from, to, includeCancelled), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error!);
    }

    [HttpGet("/api/users/{userId:guid}/bookings")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> GetForUser(
        Guid userId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] bool includeCancelled = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookingService.GetForUserAsync(
            userId, new BookingQuery(from, to, includeCancelled), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error!);
    }
}
