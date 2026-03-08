using System;
using ToolsUtilities;

namespace Gum.ProjectServices.FontGeneration;

/// <summary>
/// Callbacks for reporting font generation progress and requesting UI feedback.
/// All members have default no-op implementations, so callers only override what they need.
/// </summary>
public interface IFontGenerationCallbacks
{
    /// <summary>
    /// Called with an informational message during font generation.
    /// </summary>
    void OnOutput(string message) { }

    /// <summary>
    /// Called when a progress spinner should be shown.
    /// Returns an <see cref="IDisposable"/> whose <c>Dispose</c> hides the spinner,
    /// or <c>null</c> if no spinner is displayed.
    /// </summary>
    IDisposable? ShowSpinner() => null;

    /// <summary>
    /// Called before a font-related file is written so that file-watch listeners can ignore the change.
    /// </summary>
    void OnIgnoreFileChange(FilePath filePath) { }
}
