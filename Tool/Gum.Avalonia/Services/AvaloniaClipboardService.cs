using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Gum.Services;

namespace Gum.Avalonia.Services;

/// <inheritdoc cref="IClipboardService"/>
public class AvaloniaClipboardService : IClipboardService
{
    /// <inheritdoc/>
    public void SetText(string text)
    {
        Window? window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window != null)
        {
            // The contract is fire-and-forget; the clipboard write completes on the UI thread.
            _ = TopLevel.GetTopLevel(window)?.Clipboard?.SetTextAsync(text);
        }
    }
}
