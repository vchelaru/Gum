using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Gum.Dialogs;
using System.ComponentModel;

namespace Gum.Avalonia.Settings;

/// <summary>
/// The Application > Appearance page of the Settings window: the rows of <see cref="AppearanceRows"/> over
/// the <see cref="ThemingDialogViewModel"/> that is its DataContext. The look is the shared
/// <see cref="SettingsRowsPageView"/>.
/// </summary>
public sealed class AppearanceSettingsPageView : SettingsRowsPageView
{
    private readonly Button _resetAll;
    private ThemingDialogViewModel? _viewModel;

    /// <summary>Builds the view, with Reset All below the rows.</summary>
    public AppearanceSettingsPageView()
    {
        _resetAll = new Button
        {
            Name = "ResetAllButton",
            Content = "Reset All",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 16, 0, 0),
            IsEnabled = false,
        };
        _resetAll.Click += (_, _) => _viewModel?.ResetCommand.Execute(null);
        SetDock(_resetAll, Dock.Bottom);
        Children.Insert(0, _resetAll);
    }

    /// <inheritdoc/>
    protected override void HandleDataContextChanged()
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= HandleViewModelChanged;
        }
        _viewModel = DataContext as ThemingDialogViewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += HandleViewModelChanged;
        }
        _resetAll.IsEnabled = _viewModel?.HasAnyExplicitColor == true;
        Rows = _viewModel != null ? new AppearanceRows(_viewModel) : null;
    }

    private void HandleViewModelChanged(object? sender, PropertyChangedEventArgs e) =>
        _resetAll.IsEnabled = _viewModel?.HasAnyExplicitColor == true;
}
