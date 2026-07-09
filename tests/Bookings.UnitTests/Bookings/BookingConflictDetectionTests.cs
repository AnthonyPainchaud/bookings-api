using Bookings.Application.Bookings;
using Bookings.Application.Bookings.Dtos;
using Bookings.Application.Common.Results;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using Bookings.Infrastructure.Persistence;
using Bookings.UnitTests.TestSupport;
using Xunit;

namespace Bookings.UnitTests.Bookings;

/// <summary>
/// Exercises <see cref="BookingService.CreateAsync"/>'s overlap detection against
/// every interval-overlap shape. These run against the real service and a real
/// <see cref="BookingsDbContext"/> (EF Core InMemory provider), so they verify
/// actual production logic rather than a re-implementation of it.
/// </summary>
public class BookingConflictDetectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExistingStart = new(2026, 1, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExistingEnd = new(2026, 1, 10, 11, 0, 0, TimeSpan.Zero);

    public static IEnumerable<object[]> OverlapCases()
    {
        // candidateStart, candidateEnd, expectConflict, description
        yield return new object[] { ExistingStart, ExistingEnd, true, "exact match" };
        yield return new object[] { ExistingStart.AddMinutes(30), ExistingEnd.AddMinutes(30), true, "starts during the existing booking" };
        yield return new object[] { ExistingStart.AddMinutes(-30), ExistingEnd.AddMinutes(-30), true, "ends during the existing booking" };
        yield return new object[] { ExistingStart.AddMinutes(-15), ExistingEnd.AddMinutes(15), true, "fully contains the existing booking" };
        yield return new object[] { ExistingStart.AddMinutes(15), ExistingEnd.AddMinutes(-15), true, "fully contained by the existing booking" };
        yield return new object[] { ExistingStart.AddHours(-1), ExistingStart, false, "ends exactly when the existing one starts (adjacent, half-open)" };
        yield return new object[] { ExistingEnd, ExistingEnd.AddHours(1), false, "starts exactly when the existing one ends (adjacent, half-open)" };
    }

    [Theory]
    [MemberData(nameof(OverlapCases))]
    public async Task CreateAsync_evaluates_every_overlap_shape_correctly(
        DateTimeOffset candidateStart, DateTimeOffset candidateEnd, bool expectConflict, string because)
    {
        var (service, resourceId, userId, _) = await SeedAsync();

        var result = await service.CreateAsync(userId, new CreateBookingRequest(resourceId, candidateStart, candidateEnd, null));

        if (expectConflict)
        {
            Assert.False(result.IsSuccess, because);
            Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        }
        else
        {
            Assert.True(result.IsSuccess, because);
        }
    }

    [Fact]
    public async Task CreateAsync_allows_overlap_when_the_existing_booking_is_cancelled()
    {
        var (service, resourceId, userId, _) = await SeedAsync(existingStatus: BookingStatus.Cancelled);

        var result = await service.CreateAsync(userId, new CreateBookingRequest(resourceId, ExistingStart, ExistingEnd, null));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_allows_the_same_time_range_on_a_different_resource()
    {
        var (service, _, userId, dbContext) = await SeedAsync();

        var otherResource = new Resource
        {
            Id = Guid.NewGuid(),
            Name = "Room B",
            Type = ResourceType.MeetingRoom,
            Capacity = 4,
            IsActive = true,
            CreatedAt = Now
        };
        dbContext.Resources.Add(otherResource);
        await dbContext.SaveChangesAsync();

        var result = await service.CreateAsync(userId, new CreateBookingRequest(otherResource.Id, ExistingStart, ExistingEnd, null));

        Assert.True(result.IsSuccess);
    }

    private static async Task<(BookingService Service, Guid ResourceId, Guid UserId, BookingsDbContext DbContext)> SeedAsync(
        BookingStatus existingStatus = BookingStatus.Confirmed)
    {
        var dbContext = InMemoryDbContextFactory.Create();
        var timeProvider = new FakeTimeProvider(Now);

        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            Name = "Room A",
            Type = ResourceType.MeetingRoom,
            Capacity = 4,
            IsActive = true,
            CreatedAt = Now
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "alice@example.com",
            FullName = "Alice",
            PasswordHash = "hash",
            CreatedAt = Now
        };
        var existingBooking = new Booking
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            UserId = user.Id,
            StartsAt = ExistingStart,
            EndsAt = ExistingEnd,
            Status = existingStatus,
            CreatedAt = Now
        };

        dbContext.Resources.Add(resource);
        dbContext.Users.Add(user);
        dbContext.Bookings.Add(existingBooking);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, timeProvider);
        return (service, resource.Id, user.Id, dbContext);
    }
}
