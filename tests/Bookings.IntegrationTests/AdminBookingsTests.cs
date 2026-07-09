using System.Net;
using System.Net.Http.Json;
using Bookings.Application.Bookings.Dtos;
using Bookings.Application.Common.Pagination;
using Bookings.IntegrationTests.TestSupport;
using Xunit;

namespace Bookings.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class AdminBookingsTests
{
    private readonly HttpClient _client;

    public AdminBookingsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task A_regular_user_cannot_list_all_bookings()
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);

        var response = await _client.GetAsync("/api/v1/admin/bookings");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_request_is_rejected()
    {
        var response = await _client.GetAsync("/api/v1/admin/bookings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_sees_a_booking_made_by_another_user_with_resolved_names()
    {
        var admin = await _client.LoginAsSeedAdminAsync();
        _client.AuthorizeWith(admin.AccessToken);
        var resource = await _client.CreateResourceAsync(name: $"Admin-View Room {Guid.NewGuid():N}");

        var owner = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(owner.AccessToken);
        var start = DateTimeOffset.UtcNow.AddDays(6);
        var created = await _client.CreateBookingAsync(resource.Id, start, start.AddHours(1));
        created.EnsureSuccessStatusCode();
        var booking = await created.Content.ReadAsBookingAsync();

        _client.AuthorizeWith(admin.AccessToken);
        var page = await _client.GetFromJsonAsync<PagedResult<AdminBookingResponse>>(
            $"/api/v1/admin/bookings?resourceId={resource.Id}", TestJson.Options);

        var entry = Assert.Single(page!.Items);
        Assert.Equal(booking!.Id, entry.Id);
        Assert.Equal(resource.Name, entry.ResourceName);
        Assert.Equal(owner.User.Email, entry.UserEmail);
    }
}
