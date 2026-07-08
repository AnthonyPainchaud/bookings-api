using Bookings.Application.Resources.Dtos;
using Bookings.Domain.Entities;

namespace Bookings.Application.Resources;

/// <summary>
/// Explicit hand-written mapping between the <see cref="Resource"/> entity and
/// its DTOs. Explicit mapping (vs. a convention-based mapper) keeps the
/// translation obvious and easy to grep.
/// </summary>
internal static class ResourceMappings
{
    public static ResourceResponse ToResponse(this Resource resource) => new(
        resource.Id,
        resource.Name,
        resource.Description,
        resource.Type,
        resource.Capacity,
        resource.IsActive,
        resource.CreatedAt,
        resource.UpdatedAt);
}
