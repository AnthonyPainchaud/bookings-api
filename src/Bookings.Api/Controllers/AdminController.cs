using Asp.Versioning;
using Bookings.Api.Common;
using Bookings.Application.Bookings;
using Bookings.Application.Bookings.Dtos;
using Bookings.Application.Common.Pagination;
using Bookings.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Api.Controllers;

/// <summary>Admin-only reporting endpoints. Every action here requires the Admin role.</summary>
[Authorize(Roles = nameof(UserRole.Admin))]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
public class AdminController : ApiControllerBase
{
    private readonly IBookingService _bookingService;

    public AdminController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>Lists bookings across every resource and every user, newest first.</summary>
    [HttpGet("bookings")]
    [ProducesResponseType(typeof(PagedResult<AdminBookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AdminBookingResponse>>> GetAllBookings(
        [FromQuery] Guid? resourceId,
        [FromQuery] Guid? userId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] bool includeCancelled,
        [FromQuery] PaginationParameters pagination,
        CancellationToken cancellationToken)
    {
        var query = new AdminBookingQuery(resourceId, userId, from, to, includeCancelled, pagination.Page, pagination.PageSize);
        var result = await _bookingService.GetAllForAdminAsync(query, cancellationToken);

        return Ok(result);
    }
}
