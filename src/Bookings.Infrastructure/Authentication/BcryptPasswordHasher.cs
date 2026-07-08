using Bookings.Application.Common.Interfaces;

namespace Bookings.Infrastructure.Authentication;

/// <summary>
/// <see cref="IPasswordHasher"/> backed by BCrypt (via BCrypt.Net-Next). BCrypt
/// applies a per-hash random salt and an adjustable work factor, so identical
/// passwords produce different hashes and brute-forcing stays expensive.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    // Cost 12 ≈ a few hundred ms per hash on current hardware — a deliberate
    // balance between login latency and resistance to offline cracking.
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (Exception ex) when (ex is ArgumentException or BCrypt.Net.SaltParseException)
        {
            // A stored hash that isn't a valid BCrypt string can never match — treat
            // it as a failed verification rather than letting it surface as a 500.
            return false;
        }
    }
}
