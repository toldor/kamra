namespace KamraApp.Unit.Tests;

// A clock that always returns the same instant, so date rules are tested on a fixed day.
internal sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
