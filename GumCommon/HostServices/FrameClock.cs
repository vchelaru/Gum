using System;

namespace Gum;

/// <summary>
/// Turns the total seconds an <c>IGumService.Update(double)</c> call receives into the two
/// <see cref="TimeSpan"/> values of an engine's <c>GameTime</c> (total and elapsed). Shared by the hosts
/// whose <c>Update</c> takes a <c>GameTime</c>.
/// </summary>
internal static class FrameClock
{
    // Far enough from TimeSpan's limit (about 9.2e11 seconds) that the difference of two clamped values
    // cannot overflow either.
    private const double MaxSeconds = 4e11;

    /// <summary>
    /// Converts <paramref name="totalSeconds"/> without throwing. Elapsed is the gap since
    /// <paramref name="previousTotal"/>, and zero when there is no previous frame. A value that is not a
    /// finite number is a frame of zero length: the total stays at the previous one. A very large value is
    /// clamped.
    /// </summary>
    public static void ToTimeSpans(double totalSeconds, TimeSpan? previousTotal, out TimeSpan total, out TimeSpan elapsed)
    {
        if (double.IsNaN(totalSeconds) || double.IsInfinity(totalSeconds))
        {
            total = previousTotal ?? TimeSpan.Zero;
            elapsed = TimeSpan.Zero;
            return;
        }

        total = TimeSpan.FromSeconds(Math.Max(-MaxSeconds, Math.Min(MaxSeconds, totalSeconds)));
        elapsed = previousTotal.HasValue ? total - previousTotal.Value : TimeSpan.Zero;
    }
}
