using Bookings.Application.Common.Exceptions;
using Bookings.Application.Common.Interfaces;
using Bookings.Application.Common.Results;
using Bookings.Application.Users.Dtos;
using Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Application.Users;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public UserService(IApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

        return users.Select(u => u.ToResponse()).ToList();
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return user?.ToResponse();
    }

    public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        // Fast, friendly pre-check. The unique index on Email is the ultimate
        // guarantee and is handled below in case of a concurrent insert.
        var emailTaken = await _dbContext.Users
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailTaken)
        {
            return Error.Conflict($"A user with email '{email}' already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = request.FullName.Trim(),
            CreatedAt = _timeProvider.GetUtcNow()
        };

        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (ConflictException)
        {
            return Error.Conflict($"A user with email '{email}' already exists.");
        }

        return Result<UserResponse>.Success(user.ToResponse());
    }
}
