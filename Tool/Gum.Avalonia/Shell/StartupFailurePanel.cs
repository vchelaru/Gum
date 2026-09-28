using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gum.Avalonia.Shell;

/// <summary>
/// What the main window shows in place of its panels when startup fails, so an unattended run's
/// screenshot captures the reason.
/// </summary>
public sealed class StartupFailurePanel : ScrollViewer
{
    /// <summary>Creates the panel for <paramref name="exception"/>.</summary>
    public StartupFailurePanel(Exception exception)
    {
        Message = new TextBlock
        {
            Text = "Startup failed:\n\n" + exception,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16),
        };
        Content = Message;
    }

    /// <summary>The failure text.</summary>
    internal TextBlock Message { get; }
}
