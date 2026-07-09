using System.Net;
using System.Net.Http.Json;
using Bookings.Application.Bookings.Dtos;
using Bookings.Application.Common.Pagination;
using Bookings.Application.Resources.Dtos;
using Bookings.IntegrationTests.TestSupport;
using Xunit;

namespace Bookings.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class PaginationTests
{
    private readonly HttpClient _client;

    public PaginationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Resource_list_pages_correctly_with_no_overlap_between_pages()
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);

        // A unique prefix isolates this test's rows from anything the shared
        // database already has from other tests in the run.
        var prefix = Guid.NewGuid().ToString("N");
        for (var i = 0; i < 25; i++)
        {
            await _client.CreateResourceAsync(name: $"{prefix}-{i:D2}");
        }

        var all = await _client.GetFromJsonAsync<PagedResult<ResourceResponse>>(
            "/api/v1/resources?pageSize=100", TestJson.Options);
        Assert.Equal(25, all!.Items.Count(r => r.Name.StartsWith(prefix)));

        var page1 = await _client.GetFromJsonAsync<PagedResult<ResourceResponse>>(
            "/api/v1/resources?page=1&pageSize=10", TestJson.Options);
        var page2 = await _client.GetFromJsonAsync<PagedResult<ResourceResponse>>(
            "/api/v1/resources?page=2&pageSize=10", TestJson.Options);

        Assert.Equal(10, page1!.Items.Count);
        Assert.Equal(10, page2!.Items.Count);
        Assert.Empty(page1.Items.Select(r => r.Id).Intersect(page2.Items.Select(r => r.Id)));
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task Invalid_pagination_parameters_return_400(string query)
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);

        var response = await _client.GetAsync($"/api/v1/resources?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task My_bookings_reports_the_correct_total_count()
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);
        var resource = await _client.CreateResourceAsync();

        var baseStart = DateTimeOffset.UtcNow.AddDays(5);
        for (var i = 0; i < 3; i++)
        {
            var start = baseStart.AddHours(i * 2);
            var response = await _client.CreateBookingAsync(resource.Id, start, start.AddHours(1));
            response.EnsureSuccessStatusCode();
        }

        var page = await _client.GetFromJsonAsync<PagedResult<BookingResponse>>(
            "/api/v1/me/bookings?pageSize=100", TestJson.Options);

        Assert.Equal(3, page!.TotalCount);
        Assert.Equal(3, page.Items.Count);
    }
}
