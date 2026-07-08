using System.ComponentModel.DataAnnotations;
using Bookings.Domain.Enums;

namespace Bookings.Application.Resources.Dtos;

/// <summary>
/// Payload for replacing a resource's mutable fields (full-update PUT semantics).
/// Validation attributes target the record's constructor parameters directly so
/// the [ApiController] pipeline validates them correctly.
/// </summary>
public record UpdateResourceRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Name,

    [StringLength(1000)]
    string? Description,

    [EnumDataType(typeof(ResourceType))]
    ResourceType Type,

    [Range(0, int.MaxValue)]
    int Capacity,

    bool IsActive);
