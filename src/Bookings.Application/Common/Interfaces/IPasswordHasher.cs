namespace Bookings.Application.Common.Interfaces;

/// <summary>
/// Hashes and verifies passwords. Implemented in Infrastructure with a vetted
/// algorithm (BCrypt); the Application layer never sees the algorithm details.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
