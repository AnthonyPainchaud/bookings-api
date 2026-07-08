using Bookings.Application.Common.Results;
using Bookings.Application.Users.Dtos;

namespace Bookings.Application.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a user. Fails with a conflict if the email is already registered.</summary>
    Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
}
