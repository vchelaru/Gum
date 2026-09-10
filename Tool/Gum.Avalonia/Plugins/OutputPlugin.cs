using System;
using System.ComponentModel.Composition;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;

namespace Gum.Avalonia.Plugins;

/// <summary>The Output tab: the shared <see cref="MainOutputViewModel"/> in an Avalonia view. Twin of <c>MainOutputPlugin</c>.</summary>
[Export(typeof(PluginBase))]
public class OutputPlugin : PluginBase, IPriorityPlugin
{
    private readonly MainOutputViewModel _viewModel;

    /// <summary>Creates the plugin over the shared output view model.</summary>
    [ImportingConstructor]
    public OutputPlugin(MainOutputViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    /// <inheritdoc/>
    public override string FriendlyName => "Output";

    /// <inheritdoc/>
    public override Version Version => new Version();

    /// <inheritdoc/>
    public override void StartUp()
    {
        OutputView view = new OutputView { DataContext = _viewModel };
        IPluginTab tab = _tabManager.AddControl(view, "Output", TabLocation.RightBottom);
        // Errors written to Output are silent otherwise, so bring the tab forward.
        _viewModel.ErrorAdded += () =>
        {
            tab.Show();
            tab.IsSelected = true;
        };
    }

    /// <inheritdoc/>
    public override bool ShutDown(PluginShutDownReason shutDownReason) => false;
}

/// <summary>A clear button above a read-only, auto-scrolling text box bound to the output text.</summary>
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
