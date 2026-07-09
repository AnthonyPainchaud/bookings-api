using System.Net;
using Bookings.IntegrationTests.TestSupport;
using Xunit;

namespace Bookings.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class AuthorizationTests
{
    private readonly HttpClient _client;

    public AuthorizationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Listing_resources_without_a_token_returns_401()
    {
        var response = await _client.GetAsync("/api/v1/resources");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_booking_without_a_token_returns_401()
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);

        var response = await _client.CreateBookingAsync(Guid.NewGuid(), start, start.AddHours(1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_request_with_a_malformed_token_returns_401()
    {
        _client.AuthorizeWith("not-a-valid-jwt");

        var response = await _client.GetAsync("/api/v1/resources");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_user_cannot_view_another_users_booking()
    {
        var owner = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(owner.AccessToken);
        var resource = await _client.CreateResourceAsync();
        var start = DateTimeOffset.UtcNow.AddDays(4);
        var created = await _client.CreateBookingAsync(resource.Id, start, start.AddHours(1));
        created.EnsureSuccessStatusCode();
        var booking = await created.Content.ReadAsBookingAsync();

        var intruder = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(intruder.AccessToken);

        var response = await _client.GetAsync($"/api/v1/bookings/{booking!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_user_cannot_cancel_another_users_booking()
    {
        var owner = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(owner.AccessToken);
        var resource = await _client.CreateResourceAsync();
        var start = DateTimeOffset.UtcNow.AddDays(4).AddHours(2);
        var created = await _client.CreateBookingAsync(resource.Id, start, start.AddHours(1));
        created.EnsureSuccessStatusCode();
        var booking = await created.Content.ReadAsBookingAsync();

        var intruder = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(intruder.AccessToken);

        var response = await _client.PostAsync($"/api/v1/bookings/{booking!.Id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
