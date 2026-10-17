using Avalonia.Controls;
using AvaloniaDataUi;
using CodeOutputPlugin;

namespace Gum.Avalonia.Settings;

/// <summary>
/// The Code generation page of the Settings window: a property grid over the project-wide rows of
/// its <see cref="CodeGenerationSettingsViewModel"/>, rebuilt when a setting changes which rows exist.
/// </summary>
public sealed class CodeGenerationSettingsPageView : DockPanel
{
    private CodeGenerationSettingsViewModel? _viewModel;

    /// <summary>Builds the view.</summary>
    public CodeGenerationSettingsPageView()
    {
        Grid = new DataUiGrid { HideLoneCategoryHeader = true };
        Children.Add(Grid);

        DataContextChanged += (_, _) => HandleDataContextChanged();
    }

    /// <summary>The property grid.</summary>
    public DataUiGrid Grid { get; }

    private void HandleDataContextChanged()
    {
        if (_viewModel != null)
        {
            _viewModel.Reloaded -= Rebuild;
        }
        _viewModel = DataContext as CodeGenerationSettingsViewModel;
        if (_viewModel != null)
        {
            _viewModel.Reloaded += Rebuild;
        }
        Rebuild();
    }

    private void Rebuild()
    {
        if (_viewModel != null)
        {
            Grid.SetCategories(_viewModel.BuildCategories());
        }
    }
}
