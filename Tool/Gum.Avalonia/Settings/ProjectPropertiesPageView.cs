using Gum.Plugins.PropertiesWindowPlugin;

namespace Gum.Avalonia.Settings;

/// <summary>
/// One Project page of the Settings window (General, Canvas or Localization), each built from rows
/// (<see cref="ProjectGeneralRows"/>, <see cref="CanvasRows"/>, <see cref="LocalizationRows"/>) like the Code
/// generation page, over the settings of their <see cref="ProjectPropertiesPageViewModel"/>.
/// </summary>
public sealed class ProjectPropertiesPageView : SettingsRowsPageView
{
    /// <inheritdoc/>
    protected override void HandleDataContextChanged()
    {
        Rows = null;
        if (DataContext is not ProjectPropertiesPageViewModel page)
        {
            return;
        }
        Rows = page.Section switch
        {
            ProjectPropertiesSection.General => new ProjectGeneralRows(page.Properties),
            ProjectPropertiesSection.Canvas => new CanvasRows(page.Properties),
            _ => new LocalizationRows(page.Properties),
        };
    }
}
