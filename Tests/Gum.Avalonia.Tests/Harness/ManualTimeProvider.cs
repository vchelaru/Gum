namespace Gum.Avalonia.Tests.Harness;

/// <summary>A clock that moves only when a test advances it.</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private long _timestamp;

    /// <inheritdoc/>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc/>
    public override long GetTimestamp() => _timestamp;

    /// <summary>Moves the clock forward by <paramref name="amount"/>.</summary>
    public void Advance(TimeSpan amount) => _timestamp += amount.Ticks;
}
