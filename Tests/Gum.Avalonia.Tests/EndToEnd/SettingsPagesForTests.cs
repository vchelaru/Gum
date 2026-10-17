using Gum.Plugins.PropertiesWindowPlugin;
using Gum.SettingsWindow;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>Reaches the view models behind the Settings window's pages, as the plugins registered them.</summary>
internal static class SettingsPagesForTests
{
    /// <summary>The page registered with <paramref name="pageId"/>.</summary>
    public static SettingsPage Page(string pageId) =>
        TestAppBuilder.Services.GetRequiredService<ISettingsPageRegistry>().Pages.Single(page => page.Id == pageId);

    /// <summary>The project settings shared by the General, Canvas and Localization project pages.</summary>
    public static ProjectPropertiesViewModel ProjectProperties() =>
        ((ProjectPropertiesPageViewModel)Page(SettingsPageIds.Canvas).Content).Properties;
}
