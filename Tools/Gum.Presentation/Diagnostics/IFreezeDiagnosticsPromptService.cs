namespace Gum.Diagnostics;

/// <summary>
/// Tells the user, once per freeze, that the watchdog captured diagnostics the previous session
/// and where they are. See <see cref="FreezeDiagnosticsPromptService"/>.
/// </summary>
public interface IFreezeDiagnosticsPromptService
{
    /// <summary>
    /// Shows the prompt when the previous session ended without a clean exit and unreported
    /// diagnostic files exist; otherwise does nothing. Call once, after the main window can own a dialog.
    /// </summary>
    void PromptIfNeeded(bool previousSessionEndedDirty);
}
