namespace Bookings.Domain.Enums;

/// <summary>
/// A user's privilege level. Admins can manage resources and see bookings
/// across all users; regular users can only manage their own bookings.
/// </summary>
public enum UserRole
{
    User = 0,
    Admin = 1
}
