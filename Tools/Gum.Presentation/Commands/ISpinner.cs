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
    /// Hides the progress indicator.
    /// </summary>
    void Hide();
}
