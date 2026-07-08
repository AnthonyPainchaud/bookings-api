using Bookings.Domain.Enums;

namespace Bookings.Domain.Entities;

/// <summary>
/// Something that can be booked for a period of time — a meeting room, a piece
/// of equipment, an appointment slot owner, and so on.
/// </summary>
public class Resource
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public ResourceType Type { get; set; }

    /// <summary>How many people/units the resource accommodates (e.g. seats in a room).</summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Whether the resource is currently offered for booking. Lets a resource be
    /// retired without deleting its historical bookings.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Bookings made against this resource.</summary>
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
