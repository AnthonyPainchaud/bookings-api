using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bookings.Application.Authentication.Dtos;
using Bookings.Application.Bookings.Dtos;
using Bookings.Application.Resources.Dtos;
using Bookings.Domain.Enums;

namespace Bookings.IntegrationTests.TestSupport;

/// <summary>
/// Small helpers that keep test "arrange" sections readable — each wraps a
/// real HTTP call through the same endpoints a real client would use.
/// </summary>
public static class ApiClientExtensions
{
    /// <summary>Registers a brand-new user with a unique email and returns the auth result.</summary>
    public static async Task<AuthResponse> RegisterNewUserAsync(this HttpClient client, string? password = null)
    {
        var request = new RegisterRequest($"{Guid.NewGuid():N}@example.com", "Test User", password ?? "S3curePassw0rd");
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options))!;
    }

    /// <summary>Attaches a bearer token to every subsequent request made with this client.</summary>
    public static void AuthorizeWith(this HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    /// <summary>Creates a resource and returns it. Assumes the client is already authorized.</summary>
    public static async Task<ResourceResponse> CreateResourceAsync(
        this HttpClient client, string? name = null, ResourceType type = ResourceType.MeetingRoom, int capacity = 4)
    {
        var request = new CreateResourceRequest(name ?? $"Room {Guid.NewGuid():N}", null, type, capacity);
        var response = await client.PostAsJsonAsync("/api/v1/resources", request, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ResourceResponse>(TestJson.Options))!;
    }

    /// <summary>Attempts to create a booking, returning the raw response so callers can assert on status codes.</summary>
    public static Task<HttpResponseMessage> CreateBookingAsync(
        this HttpClient client, Guid resourceId, DateTimeOffset startsAt, DateTimeOffset endsAt, string? notes = null)
    {
        var request = new CreateBookingRequest(resourceId, startsAt, endsAt, notes);
        return client.PostAsJsonAsync("/api/v1/bookings", request, TestJson.Options);
    }

    public static Task<BookingResponse?> ReadAsBookingAsync(this HttpContent content) =>
        content.ReadFromJsonAsync<BookingResponse>(TestJson.Options);
}
