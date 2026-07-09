using Asp.Versioning;
using Bookings.Api.Common;
using Bookings.Application.Bookings;
using Bookings.Application.Bookings.Dtos;
using Bookings.Application.Common.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class BookingsController : ApiControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>Creates a booking for the current user. Returns 409 if the slot is taken.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponse>> Create(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await _bookingService.CreateAsync(userId, request, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : HandleError(result.Error!);
    }

    /// <summary>Gets one of the current user's bookings.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await _bookingService.GetByIdAsync(userId, id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error!);
    }

    /// <summary>Cancels one of the current user's bookings, freeing the slot. Idempotent.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await _bookingService.CancelAsync(userId, id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error!);
    }

    /// <summary>Lists a resource's bookings (its schedule).</summary>
    [HttpGet("/api/v{version:apiVersion}/resources/{resourceId:guid}/bookings")]
    [ProducesResponseType(typeof(PagedResult<BookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<BookingResponse>>> GetForResource(
        Guid resourceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] bool includeCancelled,
        [FromQuery] PaginationParameters pagination,
        CancellationToken cancellationToken)
    {
        var query = new BookingQuery(from, to, includeCancelled, pagination.Page, pagination.PageSize);
        var result = await _bookingService.GetForResourceAsync(resourceId, query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error!);
    }

    /// <summary>Lists the current user's bookings.</summary>
    [HttpGet("/api/v{version:apiVersion}/me/bookings")]
    [ProducesResponseType(typeof(PagedResult<BookingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BookingResponse>>> GetMine(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] bool includeCancelled,
        [FromQuery] PaginationParameters pagination,
        CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var query = new BookingQuery(from, to, includeCancelled, pagination.Page, pagination.PageSize);
        var bookings = await _bookingService.GetForUserAsync(userId, query, cancellationToken);

        return Ok(bookings);
    }
}
