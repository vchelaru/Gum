using System;
using System.Collections.Generic;

namespace Gum.Avalonia.Canvas;

/// <summary>
/// Decides when the tool's canvases draw (#4989). A canvas draws only after something could have
/// changed what it shows, or while something animates, instead of every frame. Code that changes
/// the canvas without user input calls <see cref="RequestRedraw"/>; anything that changes on its
/// own over time registers a continuous source.
/// </summary>
public interface ICanvasRedrawScheduler
{
    /// <summary>Raised on every <see cref="RequestRedraw"/>.</summary>
    event Action? RedrawRequested;

    /// <summary>
    /// Marks every canvas as needing to draw. They keep drawing for
    /// <see cref="CanvasRedrawScheduler.SettleDuration"/>, so work that lands a few frames later
    /// (a deferred layout, a hover highlight) still shows.
    /// </summary>
    void RequestRedraw();

    /// <summary>Registers a check that keeps every canvas drawing each frame while it returns true.</summary>
    void AddContinuousRedrawSource(Func<bool> isChanging);

    /// <summary>Whether a canvas should draw this frame.</summary>
    bool IsRedrawNeeded { get; }
}

/// <inheritdoc/>
public sealed class CanvasRedrawScheduler : ICanvasRedrawScheduler
{
    /// <summary>How long canvases keep drawing after the last request.</summary>
    public static readonly TimeSpan SettleDuration = TimeSpan.FromSeconds(1);

    private readonly TimeProvider _timeProvider;
    private readonly List<Func<bool>> _continuousSources;
    private long? _lastRequestTimestamp;

    /// <summary>Creates the scheduler over the clock that times the settle period.</summary>
    public CanvasRedrawScheduler(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _continuousSources = new List<Func<bool>>();
    }

    /// <inheritdoc/>
    public event Action? RedrawRequested;

    /// <inheritdoc/>
    public void RequestRedraw()
    {
        _lastRequestTimestamp = _timeProvider.GetTimestamp();
        RedrawRequested?.Invoke();
    }

    /// <inheritdoc/>
    public void AddContinuousRedrawSource(Func<bool> isChanging) => _continuousSources.Add(isChanging);

    /// <inheritdoc/>
    public bool IsRedrawNeeded => IsSettling || IsAnyContinuousSourceChanging();

    private bool IsSettling =>
        _lastRequestTimestamp is long timestamp && _timeProvider.GetElapsedTime(timestamp) < SettleDuration;

    private bool IsAnyContinuousSourceChanging()
    {
        foreach (Func<bool> isChanging in _continuousSources)
        {
            if (isChanging())
            {
                return true;
            }
        }
        return false;
    }
}
