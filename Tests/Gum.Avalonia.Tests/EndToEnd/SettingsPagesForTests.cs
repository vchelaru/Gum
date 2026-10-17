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

    /// <summary>Turns the Standards palette on or off through the Settings window's General page.</summary>
    public static void SetStandardsPalette(ProjectTreeHarness tree, bool on)
    {
        tree.Dialogs.AnswerNextSettingsPage<GeneralSettingsViewModel>(SettingsPageIds.General, page =>
        {
            page.UseStandardsPalette = on;
            return true;
        });
        tree.PickMainMenu("Edit", "Settings...");
    }

    /// <summary>The project settings shared by the General, Canvas and Localization project pages.</summary>
    public static ProjectPropertiesViewModel ProjectProperties() =>
        ((ProjectPropertiesPageViewModel)Page(SettingsPageIds.Canvas).Content).Properties;
}
