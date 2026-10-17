using Avalonia.Styling;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.SettingsWindow;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// Every page of the Settings window over a loaded project, at every zoom level, for a PR's images
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter SettingsWindowScreenshotTests</c>). The window is new,
/// so there is no "before"; the pages it replaces are the Project Properties, Font Settings, Hotkeys and
/// Code tabs and the Theming and Manage Plugins dialogs.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class SettingsWindowScreenshotTests
{
    private static readonly (string PageId, string Name)[] Pages =
    {
        (SettingsPageIds.General, "general"),
        (SettingsPageIds.Appearance, "appearance"),
        (SettingsPageIds.KeyboardShortcuts, "keyboard-shortcuts"),
        (SettingsPageIds.Plugins, "plugins"),
        (SettingsPageIds.ProjectGeneral, "project-general"),
        (SettingsPageIds.Canvas, "project-canvas"),
        (SettingsPageIds.Fonts, "project-fonts"),
        (SettingsPageIds.Localization, "project-localization"),
        (SettingsPageIds.CodeGeneration, "project-code-generation"),
    };

    [SkippableFact]
    public void EveryPage_Dark() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Dark);

        foreach ((string pageId, string name) in Pages)
        {
            viewModel.SelectPage(pageId);
            PrScreenshot.SaveWindowAtZoomLevels(window.Window, $"settings-{name}");
        }
    });

    [SkippableFact]
    public void FirstPage_Light() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Light);

        viewModel.SelectPage(SettingsPageIds.Canvas);
        window.Save("settings-project-canvas-light");
    });

    [SkippableFact]
    public void Search_NarrowsThePageList() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Dark);

        viewModel.SearchText = "co";
        window.Save("settings-search");
    });

    [SkippableFact]
    public void WithoutAProject_ListsOnlyTheApplicationPages() => PrScreenshot.Run(() =>
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Dark);

        window.Save("settings-no-project");
    });
}
