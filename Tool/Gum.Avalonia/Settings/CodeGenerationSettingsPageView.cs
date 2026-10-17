using Avalonia.Controls;
using AvaloniaDataUi;
using Avalonia;
using Avalonia.VisualTree;
using CodeOutputPlugin;

namespace Gum.Avalonia.Settings;

/// <summary>
/// The Code generation page of the Settings window: a property grid over the project-wide rows of
/// its <see cref="CodeGenerationSettingsViewModel"/>, rebuilt when a setting changes which rows exist.
/// </summary>
public sealed class CodeGenerationSettingsPageView : DockPanel
{
    private CodeGenerationSettingsViewModel? _viewModel;
    private bool _isListening;

    /// <summary>Builds the view.</summary>
    public CodeGenerationSettingsPageView()
    {
        Grid = new DataUiGrid { HideLoneCategoryHeader = true };
        Children.Add(Grid);

        DataContextChanged += (_, _) => HandleDataContextChanged();
    }

    /// <summary>The property grid.</summary>
    public DataUiGrid Grid { get; }

    // The view model lives as long as the plugin, while a view lives as long as its Settings window,
    // so the view listens only while it is on screen. A listener left behind by a closed window
    // would keep rebuilding that window's rows on every later change.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Listen();
        Rebuild();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopListening();
    }

    private void HandleDataContextChanged()
    {
        StopListening();
        _viewModel = DataContext as CodeGenerationSettingsViewModel;
        if (this.IsAttachedToVisualTree())
        {
            Listen();
            Rebuild();
        }
    }

    private void Listen()
    {
        if (_viewModel != null && !_isListening)
        {
            _viewModel.Reloaded += Rebuild;
            _isListening = true;
        }
    }

    private void StopListening()
    {
        if (_viewModel != null && _isListening)
        {
            _viewModel.Reloaded -= Rebuild;
            _isListening = false;
        }
    }

    private void Rebuild()
    {
        if (_viewModel != null)
        {
            Grid.SetCategories(_viewModel.BuildCategories());
        }
    }
}
