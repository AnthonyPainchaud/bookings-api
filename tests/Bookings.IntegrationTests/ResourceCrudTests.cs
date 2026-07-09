using System.Net;
using System.Net.Http.Json;
using Bookings.Application.Resources.Dtos;
using Bookings.IntegrationTests.TestSupport;
using Xunit;

namespace Bookings.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ResourceCrudTests
{
    private readonly HttpClient _client;

    public ResourceCrudTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_then_get_returns_the_same_resource()
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);

        var created = await _client.CreateResourceAsync(name: $"Conference Room {Guid.NewGuid():N}", capacity: 10);

        var getResponse = await _client.GetAsync($"/api/v1/resources/{created.Id}");
        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<ResourceResponse>(TestJson.Options);

        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.Name, fetched.Name);
        Assert.Equal(10, fetched.Capacity);
    }

    [Fact]
    public async Task Get_nonexistent_resource_returns_404()
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);

        var response = await _client.GetAsync($"/api/v1/resources/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_changes_the_resources_fields()
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);
        var created = await _client.CreateResourceAsync(capacity: 4);

        var update = new UpdateResourceRequest(created.Name, "Updated description", created.Type, 8, false);
        var response = await _client.PutAsJsonAsync($"/api/v1/resources/{created.Id}", update, TestJson.Options);
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<ResourceResponse>(TestJson.Options);

        Assert.Equal(8, updated!.Capacity);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task Delete_then_get_returns_404()
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);
        var created = await _client.CreateResourceAsync();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/resources/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/resources/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_with_invalid_payload_returns_400()
    {
        var auth = await _client.RegisterNewUserAsync();
        _client.AuthorizeWith(auth.AccessToken);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/resources",
            new CreateResourceRequest("", null, Bookings.Domain.Enums.ResourceType.MeetingRoom, -1),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
