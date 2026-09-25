namespace RenderingLibrary;

/// <summary>
/// Turns a stream of mouse-wheel delta reports into discrete zoom-level steps, at the same
/// one-step-per-notch cadence a physical mouse wheel produces. A real wheel reports one notch
/// (<see cref="NotchDelta"/> units) per event; a trackpad's two-finger scroll/pinch instead reports
/// many events per second, each with a small delta, so consuming delta magnitude directly (rather
/// than just its sign) is what keeps a trackpad gesture from racing through many zoom levels a
/// mouse wheel would only reach after many physical clicks.
/// </summary>
public class WheelZoomAccumulator
{
    /// <summary>Wheel delta representing one physical mouse-wheel notch (WPF/Avalonia convention).</summary>
    public const int NotchDelta = 120;

    float _accumulated;

    /// <summary>
    /// Feeds one wheel-delta report in. Returns 1 or -1 once the running total crosses a full
    /// notch in that direction (consuming exactly one notch's worth from the total and carrying
    /// any remainder to the next call), or 0 if no notch has been reached yet. A delta against
    /// the carried total's direction drops the total first, so a reversal never steps the old way.
    /// </summary>
    public int Consume(int delta)
    {
        if (System.Math.Sign(delta) == -System.Math.Sign(_accumulated))
        {
            _accumulated = 0;
        }

        _accumulated += delta;

        if (_accumulated >= NotchDelta)
        {
            _accumulated -= NotchDelta;
            return 1;
        }

        if (_accumulated <= -NotchDelta)
        {
            _accumulated += NotchDelta;
            return -1;
        }

        return 0;
    }
}
