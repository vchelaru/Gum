namespace Gum.Commands;

/// <summary>
/// A determinate progress indicator shown during long-running tool operations such as font
/// generation. Framework-neutral so progress-driving logic (e.g.
/// <see cref="Gum.Services.Fonts.ToolFontGenerationCallbacks"/>) stays free of the concrete WPF
/// view type. The WPF implementation is <see cref="Gum.Controls.Spinner"/>.
/// </summary>
public interface ISpinner
{
    /// <summary>
    /// Sets the total number of steps and resets completed progress to zero.
    /// Safe to call from any thread.
    /// </summary>
    void SetTotal(int total);

    /// <summary>
    /// Advances completed progress by one step. Safe to call from any thread.
    /// </summary>
    void IncrementProgress();

    /// <summary>
    /// Hides the progress indicator. Safe to call from any thread.
    /// </summary>
    void Hide();

    /// <summary>
    /// True when the indicator is its own top-level window. Hiding one can hand focus to another
    /// app, so the caller re-activates the main window afterward; indicators that are part of the
    /// main window must not trigger that, because activating a window while another app is in the
    /// foreground makes Windows flash its taskbar button.
    /// </summary>
    bool IsFloatingWindow { get; }
}
