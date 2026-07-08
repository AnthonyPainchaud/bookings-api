using System.ComponentModel.DataAnnotations;
using Bookings.Domain.Enums;

namespace Bookings.Application.Resources.Dtos;

/// <summary>
/// Payload for creating a resource. Validation attributes are enforced
/// automatically by the [ApiController] model-binding pipeline, so an invalid
/// body is rejected with a 400 before it ever reaches the service.
/// </summary>
/// <remarks>
/// The attributes are applied directly to the positional parameters (not via a
/// <c>[property:]</c> target): MVC associates validation metadata with the
/// record's constructor parameters, and attributes on a record parameter target
/// the parameter by default.
/// </remarks>
public record CreateResourceRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Name,

    [StringLength(1000)]
    string? Description,

    [EnumDataType(typeof(ResourceType))]
    ResourceType Type,

    [Range(0, int.MaxValue)]
    int Capacity);
