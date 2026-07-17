using System;

namespace Gum.Services;

/// <summary>
/// A periodic timer that raises <see cref="Tick"/> on the UI thread. Lets ViewModels in the
/// headless Gum.Presentation assembly (ADR-0005) drive timed behavior -- such as animation
/// playback -- without referencing a UI-framework-specific timer type directly.
/// </summary>
public interface IUiTimer
{
    /// <summary>Raised on the UI thread every time the running timer's interval elapses.</summary>
    event Action? Tick;

    /// <summary>Starts (or restarts with a new interval) the timer so it fires every <paramref name="interval"/>.</summary>
    void Start(TimeSpan interval);

    /// <summary>Stops the timer. No-op if it isn't currently running.</summary>
    void Stop();
}
