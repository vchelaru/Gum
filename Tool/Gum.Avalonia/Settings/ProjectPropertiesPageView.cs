using Gum.Plugins.PropertiesWindowPlugin;

namespace Gum.Avalonia.Settings;

/// <summary>
/// One Project page of the Settings window (General, Canvas or Localization). General is built from rows
/// (<see cref="ProjectGeneralRows"/>) like the Code generation page; Canvas and Localization are a
/// property grid over the settings of their <see cref="ProjectPropertiesPageViewModel"/>, laid out by the
/// shared <see cref="ProjectPropertiesGridPresenter"/>.
/// </summary>
public sealed class ProjectPropertiesPageView : SettingsRowsPageView
{
    private ProjectPropertiesGridPresenter? _presenter;

    /// <inheritdoc/>
    protected override void HandleDataContextChanged()
    {
        _presenter?.Bind(null);
        _presenter = null;
        Rows = null;
        if (DataContext is not ProjectPropertiesPageViewModel page)
        {
            return;
        }
        if (page.Section == ProjectPropertiesSection.General)
        {
            Rows = new ProjectGeneralRows(page.Properties);
        }
        else
        {
            _presenter = new ProjectPropertiesGridPresenter(Grid, page.Section);
            _presenter.Bind(page.Properties);
        }
    }
}
