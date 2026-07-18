namespace Gum.Services;

/// <summary>
/// Copies text to the system clipboard. Abstracts <c>System.Windows.Forms.Clipboard</c> so logic
/// classes can copy text without a WinForms dependency (ADR-0005). See
/// <see cref="Gum.Services.ClipboardService"/> for the concrete implementation (tool project).
/// </summary>
public interface IClipboardService
{
    /// <summary>Sets the system clipboard's text content.</summary>
    void SetText(string text);
}
