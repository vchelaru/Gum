using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Managers;
using Gum.Plugins.FileWatchPlugin;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;

namespace Gum.Avalonia.Panels;

/// <summary>
/// The Output tab: a clear button above a read-only, auto-scrolling text box bound to
/// <see cref="MainOutputViewModel"/>. Twin of the WPF <c>MainOutputPluginView</c>.
/// </summary>
public sealed class OutputView : DockPanel
{
    /// <summary>Builds the view.</summary>
    public OutputView()
    {
        Margin = new Thickness(4);

        Button clear = new Button { Content = "Clear", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 4) };
        clear.Bind(Button.CommandProperty, new Binding(nameof(MainOutputViewModel.ClearOutputCommand)));
        SetDock(clear, Dock.Top);
        Children.Add(clear);

        TextBox text = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,monospace"),
        };
        text.Bind(TextBox.TextProperty, new Binding(nameof(MainOutputViewModel.OutputText)));
        text.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                text.CaretIndex = text.Text?.Length ?? 0;
            }
        };
        Children.Add(text);
    }
}

/// <summary>The Hotkeys tab: every hotkey and its binding. Twin of the WPF <c>HotkeyView</c>.</summary>
public sealed class HotkeyView : ListBox
{
    // A ListBox subclass gets no theme (styles match the exact type) and so draws nothing.
    protected override Type StyleKeyOverride => typeof(ListBox);

    /// <summary>Builds the view.</summary>
    public HotkeyView()
    {
        this.Bind(ItemsSourceProperty, new Binding(nameof(HotkeyViewModel.Items)));
    }
}

/// <summary>
/// The File Watch debug tab: the watched folders, queued changes, flush countdown, and active
/// ignores. Twin of the WPF <c>FileWatchControl</c>.
/// </summary>
public sealed class FileWatchView : ScrollViewer
{
    // A ScrollViewer subclass gets no theme (styles match the exact type) and so draws nothing.
    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    /// <summary>Builds the view.</summary>
    public FileWatchView()
    {
        StackPanel panel = new StackPanel { Margin = new Thickness(4) };

        CheckBox print = new CheckBox { Content = "Print File Changes to Output", Margin = new Thickness(0, 0, 0, 8) };
        print.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(FileWatchViewModel.PrintFileChangesToOutput)) { Mode = BindingMode.TwoWay });
        panel.Children.Add(print);

        foreach (string property in new[]
        {
            nameof(FileWatchViewModel.WatchFolderInformation),
            nameof(FileWatchViewModel.NumberOfFilesToFlush),
            nameof(FileWatchViewModel.TimeToNextFlush),
            nameof(FileWatchViewModel.NextFilesToFlush),
            nameof(FileWatchViewModel.IgnoredFilesInformation),
        })
        {
            TextBlock line = new TextBlock();
            line.Bind(TextBlock.TextProperty, new Binding(property));
            panel.Children.Add(line);
        }

        Content = panel;
    }
}
