using Bookings.Application.Common.Interfaces;
using Bookings.Application.Users.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Application.Users;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _dbContext;

    public UserService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return user?.ToResponse();
    }
}
