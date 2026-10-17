using Avalonia.Controls;
using AvaloniaDataUi;
using Gum.Plugins.PropertiesWindowPlugin;

namespace Gum.Avalonia.Settings;

/// <summary>
/// One Project page of the Settings window (General, Canvas or Localization): a property grid over the
/// settings of its <see cref="ProjectPropertiesPageViewModel"/>, laid out by the shared
/// <see cref="ProjectPropertiesGridPresenter"/>.
/// </summary>
public sealed class ProjectPropertiesPageView : DockPanel
{
    private ProjectPropertiesGridPresenter? _presenter;

    /// <summary>Builds the view.</summary>
    public ProjectPropertiesPageView()
    {
        Grid = new DataUiGrid { HideLoneCategoryHeader = true };
        Children.Add(Grid);

        DataContextChanged += (_, _) => HandleDataContextChanged();
    }

    /// <summary>The property grid.</summary>
    public DataUiGrid Grid { get; }

    private void HandleDataContextChanged()
    {
        _presenter?.Bind(null);
        if (DataContext is ProjectPropertiesPageViewModel page)
        {
            _presenter = new ProjectPropertiesGridPresenter(Grid, page.Section);
            _presenter.Bind(page.Properties);
        }
        else
        {
            _presenter = null;
        }
    }
}
