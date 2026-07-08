namespace Bookings.Domain.Enums;

/// <summary>
/// The kind of thing a <see cref="Entities.Resource"/> represents. Keeping this
/// as an enum lets callers filter/group by category while the set of supported
/// types stays explicit and validated.
/// </summary>
public enum ResourceType
{
    MeetingRoom = 0,
    Equipment = 1,
    Appointment = 2,
    Other = 3
}
