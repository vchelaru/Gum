using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.SettingsWindow;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The real main window (Variables, Animations, States tabs), the Settings pages and several dialogs at base font
/// sizes 6, 12 and 24, to find chrome that stays a fixed size when the tool zooms (#7371)
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter ZoomFixedSizesScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ZoomFixedSizesScreenshotTests
{
    private static readonly double[] Sizes = { 6d, 12d, 24d };

    [SkippableFact]
    public void MainWindowTabs() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        InstanceSave sprite = tree.Project.AddInstance(card, "Silhouette", "Sprite");
        tree.TreeManager.RefreshUi();
        tree.Click(tree.NodeFor(sprite));

        tree.Input.Window.Content = null;
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 1700;
        window.Height = 900;
        window.RequestedThemeVariant = ThemeVariant.Dark;
        window.Show();
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        Dispatcher.UIThread.RunJobs();

        List<string> titles = new List<string>();
        foreach (TabControl tabs in window.GetVisualDescendants().OfType<TabControl>())
        {
            titles.Add(string.Join("|", tabs.Items.OfType<AvaloniaPluginTab>().Select(tab => tab.Title)));
        }
        File.WriteAllLines(Path.Combine(PrScreenshot.OutputDirectory!, "main-tabs.txt"), titles);

        foreach (string tab in new[] { "Variables", "Animations", "States" })
        {
            SelectTab(window, tab);
            SaveAtSizes(window, $"main-{tab.ToLowerInvariant()}");
        }
    });

    [SkippableFact]
    public void AnimationsTab() => PrScreenshot.Run(() =>
    {
        using Animations.AnimationEditorHarness editor = new Animations.AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", "AnimationCategory", "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk", loops: true);
        editor.AddStateKeyframe("AnimationCategory/Pressed");
        editor.AddStateKeyframe("AnimationCategory/Released");
        SaveAtSizes(editor.Window, "animations");
    });

    [SkippableFact]
    public void Dialogs() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        DeleteOptionsDialogViewModel delete = new DeleteOptionsDialogViewModel { Message = "Delete Card?" };
        delete.CheckBoxes.Add(new DeleteOptionCheckboxViewModel { Label = "Delete file", IsChecked = true });
        delete.Choices.Add(new DeleteOptionChoiceViewModel("Children", new[]
        {
            new DeleteOptionCheckboxViewModel { Label = "Delete children", IsChecked = true },
            new DeleteOptionCheckboxViewModel { Label = "Keep children" },
        }));
        using (ScreenshotWindow window = PrScreenshot.ShowDialog(delete, ThemeVariant.Dark))
        {
            SaveAtSizes(window.Window, "dialog-delete");
        }

        AddComponentDialogViewModelFactory(out Gum.Services.Dialogs.DialogViewModel add);
        using (ScreenshotWindow window = PrScreenshot.ShowDialog(add, ThemeVariant.Dark))
        {
            SaveAtSizes(window.Window, "dialog-add-component");
        }

        SettingsDialogViewModel settings = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using (ScreenshotWindow window = PrScreenshot.ShowDialog(settings, ThemeVariant.Dark))
        {
            foreach ((string pageId, string name) in new[]
            {
                (SettingsPageIds.General, "general"), (SettingsPageIds.Appearance, "appearance"),
                (SettingsPageIds.KeyboardShortcuts, "keys"), (SettingsPageIds.Plugins, "plugins"),
                (SettingsPageIds.ProjectGeneral, "project-general"), (SettingsPageIds.Canvas, "canvas"),
                (SettingsPageIds.Fonts, "fonts"), (SettingsPageIds.CodeGeneration, "codegen"),
            })
            {
                settings.SelectPage(pageId);
                SaveAtSizes(window.Window, $"settings-{name}");
            }
        }
    });

    private static void AddComponentDialogViewModelFactory(out Gum.Services.Dialogs.DialogViewModel viewModel) =>
        viewModel = TestAppBuilder.Services.GetRequiredService<Gum.Dialogs.AddComponentDialogViewModel>();

    private static void SaveAtSizes(Window window, string name)
    {
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window, scale);
        try
        {
            foreach (double size in Sizes)
            {
                scale.BaseFontSize = size;
                PrScreenshot.SaveWindow(window, $"{name}-zoom-{size}");
            }
        }
        finally
        {
            scale.BaseFontSize = originalSize;
        }
    }

    private static void SelectTab(Window window, string title)
    {
        foreach (TabControl tabs in window.GetVisualDescendants().OfType<TabControl>())
        {
            foreach (AvaloniaPluginTab tab in tabs.Items.OfType<AvaloniaPluginTab>())
            {
                if (tab.Title == title)
                {
                    tabs.SelectedItem = tab;
                }
            }
        }
        Dispatcher.UIThread.RunJobs();
    }
}
