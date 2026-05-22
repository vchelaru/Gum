using System;

namespace Gum.Forms.Controls
{
    /// <summary>
    /// Platform-agnostic abstraction for the OS clipboard. Forms controls in GumCommon —
    /// primarily <c>TextBox</c> and <c>PasswordBox</c> — use this to read and write
    /// clipboard text on copy / cut / paste without depending on a runtime-specific
    /// clipboard library (currently TextCopy on the MonoGame side, an injected
    /// implementation on Blazor).
    /// </summary>
    /// <remarks>
    /// Concrete implementations are supplied by each runtime that has a clipboard story.
    /// Platforms without one (iOS today, headless test runs) leave
    /// <c>IGumService.Clipboard</c> null and callers no-op the copy/paste operation.
    /// </remarks>
    public interface IGumClipboard
    {
        /// <summary>
        /// Reads text from the clipboard. May return <c>null</c> immediately if the read
        /// is asynchronous (e.g. the Blazor injected path) and the result is not yet
        /// available — in that case <paramref name="callback"/> is invoked when the
        /// underlying task completes, and a subsequent call returns the resolved value.
        /// </summary>
        /// <param name="callback">
        /// Optional continuation fired when an async clipboard read finishes. Ignored when
        /// the underlying clipboard returns synchronously.
        /// </param>
        string? GetText(Action? callback);

        /// <summary>
        /// Writes text to the clipboard.
        /// </summary>
        void SetText(string text);
    }
}
