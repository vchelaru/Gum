using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;

namespace Gum.Avalonia.Shell;

/// <summary>
/// What the main window shows in place of its panels when startup fails: the reason as selectable
/// text of bounded width, with Copy and Close buttons. An unattended run's screenshot captures it.
/// </summary>
public sealed class StartupFailurePanel : Grid
{
    // Room above the text for the system caption buttons, which draw over the window's top edge.
    private const double CaptionHeight = 40;
    private const double MaxTextWidth = 900;

    /// <summary>Creates the panel for <paramref name="exception"/>.</summary>
    /// <param name="exception">The startup failure.</param>
    /// <param name="close">Closes the application.</param>
    /// <param name="copy">Puts the failure text on the clipboard.</param>
    public StartupFailurePanel(Exception exception, Action close, Action<string> copy)
    {
        string text = "Startup failed:\n\n" + exception;
        Message = new SelectableTextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = MaxTextWidth,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        CopyButton = new Button { Content = "Copy", Command = new RelayCommand(() => copy(text)) };
        CloseButton = new Button { Content = "Close", Command = new RelayCommand(close) };

        StackPanel buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(16, 8, 16, 16),
            Children = { CopyButton, CloseButton },
        };
        ScrollViewer scroller = new ScrollViewer
        {
            Content = Message,
            Margin = new Thickness(16, CaptionHeight, 16, 0),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        RowDefinitions = new RowDefinitions("*,Auto");
        SetRow(scroller, 0);
        SetRow(buttons, 1);
        Children.Add(scroller);
        Children.Add(buttons);
    }

    /// <summary>The failure text.</summary>
    internal TextBlock Message { get; }

    /// <summary>Copies the failure text.</summary>
    internal Button CopyButton { get; }

    /// <summary>Closes the application.</summary>
    internal Button CloseButton { get; }
}
