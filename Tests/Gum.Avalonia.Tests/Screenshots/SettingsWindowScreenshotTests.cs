using Avalonia;
using Avalonia.Controls;
using System;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CodeOutputPlugin;
using Gum.Avalonia.Settings;
using Gum.ProjectServices.CodeGeneration;
using System.Linq;
using System.Reflection;
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
    public void CodeGeneration_Light() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Light);

        viewModel.SelectPage(SettingsPageIds.CodeGeneration);
        window.Save("settings-project-code-generation-light");
    });

    [SkippableFact]
    public void CodeGeneration_ScrolledToTheEnd_Dark() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Dark);

        viewModel.SelectPage(SettingsPageIds.CodeGeneration);
        ScrollToEnd(window);
        PrScreenshot.SaveWindowAtZoomLevels(window.Window, "settings-project-code-generation-end");
    });

    [SkippableFact]
    public void CodeGeneration_LegacyLibrary_ShowsTheCollapsedAndExpandedLegacySection() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Dark);
        viewModel.SelectPage(SettingsPageIds.CodeGeneration);

        // The legacy rows exist only for a legacy library, which the project does not use by default.
        CodeGenerationSettingsPageView page = window.Find<CodeGenerationSettingsPageView>();
        EditSettings(page, settings => settings.OutputLibrary = OutputLibrary.Maui);

        ScrollToEnd(window);
        window.Save("settings-project-code-generation-legacy-collapsed");
        page.Grid.Categories.Last().IsExpanded = true;
        ScrollToEnd(window);
        window.Save("settings-project-code-generation-legacy-expanded");
    });

    [SkippableFact]
    public void CodeGeneration_WithFolders_ShowsWhereFilesResolve() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Dark);
        viewModel.SelectPage(SettingsPageIds.CodeGeneration);

        EditSettings(window.Find<CodeGenerationSettingsPageView>(), settings =>
        {
            settings.CodeProjectRoot = "../MyGame/";
            settings.GeneratedCodeFolder = "Gum/Generated";
            settings.CustomCodeFolder = "Gum/Custom";
            settings.RootNamespace = "MyGame.Gum";
        });
        ScrollToResolves(window);
        window.Save("settings-project-code-generation-resolves");
    });

    private static void EditSettings(CodeGenerationSettingsPageView page, Action<CodeOutputProjectSettings> edit)
    {
        CodeGenerationSettingsViewModel pageModel = (CodeGenerationSettingsViewModel)page.DataContext!;
        CodeOutputSettingsMembers members = (CodeOutputSettingsMembers)typeof(CodeGenerationSettingsViewModel)
            .GetField("_members", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(pageModel)!;
        edit(members.ProjectSettings!);
        pageModel.Reload();
    }

    private static void ScrollToResolves(ScreenshotWindow window)
    {
        CodeGenerationSettingsPageView page = window.Find<CodeGenerationSettingsPageView>();
        ScrollViewer scroller = page.GetVisualAncestors().OfType<ScrollViewer>().First();
        Dispatcher.UIThread.RunJobs();
        scroller.Offset = new Vector(0, 300);
        Dispatcher.UIThread.RunJobs();
    }

    private static void ScrollToEnd(ScreenshotWindow window)
    {
        CodeGenerationSettingsPageView page = window.Find<CodeGenerationSettingsPageView>();
        ScrollViewer scroller = page.GetVisualAncestors().OfType<ScrollViewer>().First();
        // Spacing is applied after the first layout pass, so the extent grows once more before the end is final.
        for (int pass = 0; pass < 3; pass++)
        {
            Dispatcher.UIThread.RunJobs();
            window.Window.UpdateLayout();
            scroller.ScrollToEnd();
        }
    }

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
