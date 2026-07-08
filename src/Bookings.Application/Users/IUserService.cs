using Bookings.Application.Users.Dtos;

namespace Bookings.Application.Users;

public interface IUserService
{
    Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
