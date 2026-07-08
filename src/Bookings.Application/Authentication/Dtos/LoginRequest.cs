using System.ComponentModel.DataAnnotations;

namespace Bookings.Application.Authentication.Dtos;

/// <summary>Payload for exchanging credentials for an access token.</summary>
public record LoginRequest(
    [Required]
    [EmailAddress]
    string Email,

    [Required]
    string Password);
