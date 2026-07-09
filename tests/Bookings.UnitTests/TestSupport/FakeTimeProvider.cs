namespace Bookings.UnitTests.TestSupport;

/// <summary>A <see cref="TimeProvider"/> whose "now" is fixed and settable, for deterministic tests.</summary>
public sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Set(DateTimeOffset utcNow) => _utcNow = utcNow;
}
