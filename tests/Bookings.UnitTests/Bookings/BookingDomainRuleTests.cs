using Bookings.Application.Bookings;
using Bookings.Application.Bookings.Dtos;
using Bookings.Application.Common.Results;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using Bookings.UnitTests.TestSupport;
using Xunit;

namespace Bookings.UnitTests.Bookings;

/// <summary>Covers the domain rules <see cref="BookingService.CreateAsync"/> enforces beyond overlap detection.</summary>
public class BookingDomainRuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_rejects_an_end_time_that_is_not_after_the_start_time()
    {
        var (service, resourceId, userId) = await SeedAsync();
        var start = Now.AddDays(1);

        var result = await service.CreateAsync(userId, new CreateBookingRequest(resourceId, start, start, null));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_start_time_in_the_past()
    {
        var (service, resourceId, userId) = await SeedAsync();

        var result = await service.CreateAsync(
            userId, new CreateBookingRequest(resourceId, Now.AddHours(-1), Now.AddHours(1), null));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duration_longer_than_the_maximum()
    {
        var (service, resourceId, userId) = await SeedAsync();
        var start = Now.AddDays(1);

        var result = await service.CreateAsync(
            userId,
            new CreateBookingRequest(resourceId, start, start + BookingService.MaxBookingDuration + TimeSpan.FromMinutes(1), null));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task CreateAsync_returns_not_found_for_a_nonexistent_resource()
    {
        var (service, _, userId) = await SeedAsync();
        var start = Now.AddDays(1);

        var result = await service.CreateAsync(
            userId, new CreateBookingRequest(Guid.NewGuid(), start, start.AddHours(1), null));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task CreateAsync_rejects_booking_an_inactive_resource()
    {
        var dbContext = InMemoryDbContextFactory.Create();
        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            Name = "Retired Room",
            Type = ResourceType.MeetingRoom,
            Capacity = 4,
            IsActive = false,
            CreatedAt = Now
        };
        dbContext.Resources.Add(resource);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, new FakeTimeProvider(Now));
        var start = Now.AddDays(1);

        var result = await service.CreateAsync(
            Guid.NewGuid(), new CreateBookingRequest(resource.Id, start, start.AddHours(1), null));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    private static async Task<(BookingService Service, Guid ResourceId, Guid UserId)> SeedAsync()
    {
        var dbContext = InMemoryDbContextFactory.Create();
        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            Name = "Room A",
            Type = ResourceType.MeetingRoom,
            Capacity = 4,
            IsActive = true,
            CreatedAt = Now
        };
        dbContext.Resources.Add(resource);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, new FakeTimeProvider(Now));
        return (service, resource.Id, Guid.NewGuid());
    }
}
