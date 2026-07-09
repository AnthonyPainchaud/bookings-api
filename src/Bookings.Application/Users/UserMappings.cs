using Bookings.Application.Users.Dtos;
using Bookings.Domain.Entities;

namespace Bookings.Application.Users;

internal static class UserMappings
{
    public static UserResponse ToResponse(this User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.Role,
        user.CreatedAt);
}
