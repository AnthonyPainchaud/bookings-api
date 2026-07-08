using Bookings.Application.Authentication.Dtos;
using Bookings.Application.Common.Exceptions;
using Bookings.Application.Common.Interfaces;
using Bookings.Application.Common.Results;
using Bookings.Application.Users;
using Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Application.Authentication;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly TimeProvider _timeProvider;

    // A valid hash used to verify against when no user matches, so that login
    // takes the same amount of work whether or not the email exists. This
    // mitigates user-enumeration via response timing. Computed once, lazily.
    private static string? _decoyHash;

    public AuthService(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var emailTaken = await _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (emailTaken)
        {
            return Error.Conflict("An account with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = _timeProvider.GetUtcNow()
        };

        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (ConflictException)
        {
            // The unique index is the backstop for a concurrent registration.
            return Error.Conflict("An account with this email already exists.");
        }

        return Result<AuthResponse>.Success(BuildResponse(user));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Always run a verification (against a decoy hash when the user is absent)
        // to keep timing uniform, then fail with a single generic message that
        // does not reveal whether the email is registered.
        var hashToCheck = user?.PasswordHash ?? (_decoyHash ??= _passwordHasher.Hash("decoy-password"));
        var passwordValid = _passwordHasher.Verify(request.Password, hashToCheck);

        if (user is null || !passwordValid)
        {
            return Error.Unauthorized("Invalid email or password.");
        }

        return Result<AuthResponse>.Success(BuildResponse(user));
    }

    private AuthResponse BuildResponse(User user)
    {
        var token = _tokenGenerator.GenerateToken(user);
        return new AuthResponse(token.Token, token.ExpiresAt, user.ToResponse());
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
