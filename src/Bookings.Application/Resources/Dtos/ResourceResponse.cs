using Bookings.Domain.Enums;

namespace Bookings.Application.Resources.Dtos;

/// <summary>
/// The shape of a resource as returned to API clients. Kept separate from the
/// domain entity so the public contract can evolve independently of persistence.
/// </summary>
public record ResourceResponse(
    Guid Id,
    string Name,
    string? Description,
    ResourceType Type,
    int Capacity,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
