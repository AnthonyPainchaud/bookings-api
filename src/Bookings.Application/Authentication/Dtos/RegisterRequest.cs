using System.ComponentModel.DataAnnotations;

namespace Bookings.Application.Authentication.Dtos;

/// <summary>Payload for creating an account.</summary>
public record RegisterRequest(
    [Required]
    [EmailAddress]
    [StringLength(256)]
    string Email,

    [Required]
    [StringLength(200, MinimumLength = 1)]
    string FullName,

    // BCrypt only considers the first 72 bytes of a password, so the upper bound
    // is capped here to avoid silently ignoring input beyond that limit.
    [Required]
    [StringLength(72, MinimumLength = 8)]
    string Password);
