using Bookings.Application.Authentication.Dtos;
using Bookings.Application.Common.Results;

namespace Bookings.Application.Authentication;

public interface IAuthService
{
    /// <summary>Registers a new account and issues a token. Conflict if the email exists.</summary>
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Verifies credentials and issues a token. Unauthorized if they don't match.</summary>
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
