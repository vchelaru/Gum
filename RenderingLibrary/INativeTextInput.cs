using System.Threading.Tasks;

namespace Gum.Forms.Controls
{
    /// <summary>
    /// Platform-agnostic abstraction for the OS-provided modal text-input dialog
    /// (e.g. the on-screen keyboard popup shown on iOS, the soft-keyboard prompt on
    /// some console platforms). Allows Forms controls in GumCommon — most notably
    /// <c>TextBoxBase</c> — to request native text entry without depending on a
    /// specific runtime's input stack.
    /// </summary>
    /// <remarks>
    /// Concrete implementations are supplied by each runtime that has a native
    /// text-input story; runtimes that don't (Raylib, FNA, Sokol, browser, etc.)
    /// simply leave <see cref="IGumService.NativeTextInput"/> null and callers
    /// no-op. The MonoGame implementation wraps
    /// <c>Microsoft.Xna.Framework.Input.KeyboardInput.Show</c>.
    /// </remarks>
    public interface INativeTextInput
    {
        /// <summary>
        /// Shows the native text-input dialog and asynchronously returns the
        /// text the user entered, or <c>null</c> if the user cancelled.
        /// </summary>
        /// <param name="title">Short heading shown at the top of the dialog.</param>
        /// <param name="description">Secondary descriptive text shown beneath the title.</param>
        /// <param name="initialText">Pre-populated text in the input field.</param>
        /// <param name="isPassword">Whether the input should be masked (password mode).</param>
        Task<string?> ShowAsync(string title, string description, string initialText, bool isPassword);
    }
}
