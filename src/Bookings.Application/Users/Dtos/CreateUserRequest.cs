using System.ComponentModel.DataAnnotations;

namespace Bookings.Application.Users.Dtos;

/// <summary>Payload for registering a user who can make bookings.</summary>
public record CreateUserRequest(
    [Required]
    [EmailAddress]
    [StringLength(256)]
    string Email,

    [Required]
    [StringLength(200, MinimumLength = 1)]
    string FullName);
