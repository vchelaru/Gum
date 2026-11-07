using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Settings;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.SettingsWindow;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Project > General page of the Settings window with its Engine profile row, in the real window and theme, at
/// every zoom level (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter EngineProfileSettingsScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class EngineProfileSettingsScreenshotTests
{
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoProfile(bool dark) => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        Save(dark, "settings-project-general-no-engine-profile-" + (dark ? "dark" : "light"));
    });

    // A profile outside the project folder, stored with "../" segments, and the Standards palette limited by it.
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithProfile(bool dark) => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ProjectPropertiesViewModel properties = SettingsPagesForTests.ProjectProperties();
        string engineFolder = Path.Combine(Path.GetDirectoryName(tree.Project.ProjectFolder)!, "engines");
        Directory.CreateDirectory(engineFolder);
        try
        {
            File.WriteAllText(Path.Combine(engineFolder, "MonoGameSprites.gumengine"), "{ \"supportedStandardTypes\": [ \"Sprite\" ] }");
            properties.EngineProfile = Path.Combine(engineFolder, "MonoGameSprites.gumengine");
            Save(dark, "settings-project-general-engine-profile-" + (dark ? "dark" : "light"));
        }
        finally
        {
            properties.EngineProfile = string.Empty;
            Directory.Delete(engineFolder, recursive: true);
        }
    });

    private static void Save(bool dark, string name)
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, dark ? ThemeVariant.Dark : ThemeVariant.Light);
        viewModel.SelectPage(SettingsPageIds.ProjectGeneral);
        // Tall enough that the whole page shows at the largest zoom.
        window.Window.SizeToContent = SizeToContent.Manual;
        window.Window.MaxHeight = double.PositiveInfinity;
        window.Window.Height = 760;
        // A zoom change resets the scroll, so scroll to the Engine section again at each zoom level.
        ProjectPropertiesPageView page = window.Find<ProjectPropertiesPageView>();
        ScrollViewer scroller = page.GetVisualAncestors().OfType<ScrollViewer>().First();
        PrScreenshot.SaveContactSheet(window.Window, name, _ =>
        {
            for (int pass = 0; pass < 3; pass++)
            {
                Dispatcher.UIThread.RunJobs();
                window.Window.UpdateLayout();
                scroller.ScrollToEnd();
            }
        });
    }
}
