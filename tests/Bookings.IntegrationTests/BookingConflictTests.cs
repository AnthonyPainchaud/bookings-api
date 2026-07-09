using System.Net;
using Bookings.IntegrationTests.TestSupport;
using Xunit;

namespace Bookings.IntegrationTests;

/// <summary>
/// End-to-end conflict/concurrency coverage against a real PostgreSQL instance,
/// exercising the actual exclusion constraint — not a mock of it.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class BookingConflictTests
{
    private readonly HttpClient _client;

    public BookingConflictTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // Offsets (minutes) relative to an existing [0, 60) booking window.
    public static IEnumerable<object[]> OverlapOffsets()
    {
        yield return new object[] { 0, 60, "exact match" };
        yield return new object[] { 30, 90, "starts during the existing booking" };
        yield return new object[] { -30, 30, "ends during the existing booking" };
        yield return new object[] { -15, 75, "fully contains the existing booking" };
        yield return new object[] { 15, 45, "fully contained by the existing booking" };
    }

    [Theory]
    [MemberData(nameof(OverlapOffsets))]
    public async Task Overlapping_bookings_are_rejected_with_409(int offsetStartMinutes, int offsetEndMinutes, string because)
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);
        var resource = await _client.CreateResourceAsync();

        var baseStart = DateTimeOffset.UtcNow.AddDays(2);

        var first = await _client.CreateBookingAsync(resource.Id, baseStart, baseStart.AddHours(1));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.CreateBookingAsync(
            resource.Id, baseStart.AddMinutes(offsetStartMinutes), baseStart.AddMinutes(offsetEndMinutes));

        Assert.True(second.StatusCode == HttpStatusCode.Conflict, $"Expected 409 Conflict when the new booking {because}.");
    }

    [Fact]
    public async Task Back_to_back_bookings_do_not_conflict()
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);
        var resource = await _client.CreateResourceAsync();

        var start = DateTimeOffset.UtcNow.AddDays(2);
        var middle = start.AddHours(1);
        var end = middle.AddHours(1);

        var first = await _client.CreateBookingAsync(resource.Id, start, middle);
        var second = await _client.CreateBookingAsync(resource.Id, middle, end);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Cancelling_a_booking_frees_the_slot_for_rebooking()
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);
        var resource = await _client.CreateResourceAsync();

        var start = DateTimeOffset.UtcNow.AddDays(2).AddHours(3);
        var end = start.AddHours(1);

        var created = await _client.CreateBookingAsync(resource.Id, start, end);
        created.EnsureSuccessStatusCode();
        var booking = await created.Content.ReadAsBookingAsync();

        var cancelResponse = await _client.PostAsync($"/api/v1/bookings/{booking!.Id}/cancel", content: null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        var rebooked = await _client.CreateBookingAsync(resource.Id, start, end);
        Assert.Equal(HttpStatusCode.Created, rebooked.StatusCode);
    }

    [Fact]
    public async Task Concurrent_requests_for_the_same_slot_produce_exactly_one_success()
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);
        var resource = await _client.CreateResourceAsync();

        var start = DateTimeOffset.UtcNow.AddDays(3);
        var end = start.AddHours(1);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => _client.CreateBookingAsync(resource.Id, start, end)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(9, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }
}
