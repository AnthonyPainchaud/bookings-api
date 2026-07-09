using Asp.Versioning;
using Bookings.Api.Common;
using Bookings.Api.Startup;
using Bookings.Application.Authentication;
using Bookings.Application.Authentication.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bookings.Api.Controllers;

[AllowAnonymous]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
// Stricter, IP-based limit on top of the global limiter to slow down
// credential-stuffing / brute-force attempts against these anonymous endpoints.
[EnableRateLimiting(RateLimitingSetup.AuthPolicy)]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Registers a new account and returns an access token.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error!);
    }

    /// <summary>Exchanges credentials for an access token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleError(result.Error!);
    }
}
