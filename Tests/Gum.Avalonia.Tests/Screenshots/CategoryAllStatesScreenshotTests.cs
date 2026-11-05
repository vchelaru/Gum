using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Plugins.States;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.Services;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Threading;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab with a state category selected, in the real main window, for the "category lists
/// every state's variables" PR (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt;</c>): the category, then after the
/// trash can on one row. Mirrors DarkPro's Slider: the first state sets one variable, later states more.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class CategoryAllStatesScreenshotTests
{
    [SkippableFact]
    public void CategoryVariablesAtZoomLevels() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave slider = tree.Project.AddComponent("Slider");
        tree.Project.AddInstance(slider, "ThumbInstance", "Text");
        tree.Project.AddInstance(slider, "TrackBackground", "Rectangle");
        StateSaveCategory category = tree.Project.AddCategory(slider, "SliderCategory");
        StateSave enabled = tree.Project.AddState(slider, category, "Enabled");
        StateSave disabled = tree.Project.AddState(slider, category, "Disabled");
        StateSave focused = tree.Project.AddState(slider, category, "DisabledFocused");
        enabled.SetValue("ThumbInstance.Red", 10, "int");
        disabled.SetValue("TrackBackground.Red", 20, "int");
        disabled.SetValue("TrackBackground.Blue", 30, "int");
        disabled.VariableLists.Add(new VariableListSave<string> { Name = "TrackBackground.VariableReferences", Type = "string", ValueAsIList = new List<string> { "Red = ThumbInstance.Red" } });
        focused.SetValue("TrackBackground.Blue", 40, "int");
        focused.SetValue("TrackBackground.Green", 50, "int");
        tree.SaveAll();

        tree.Input.Window.Content = null;
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 1500;
        window.Height = 900;
        window.Show();
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        Dispatcher.UIThread.RunJobs();
        tree.SelectedState.SelectedElement = null;
        ScreenshotWindow.Pump(window);
        tree.SelectedState.SelectedElement = slider;
        ScreenshotWindow.Pump(window);
        StateTreeViewModel statesVm = (StateTreeViewModel)window.GetVisualDescendants().OfType<AvaloniaStateTreeView>().Single().DataContext!;
        statesVm.Categories.Single(c => c.Title == "SliderCategory").IsSelected = true;
        ScreenshotWindow.Pump(window);

        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            window.RequestedThemeVariant = theme;
            ScreenshotWindow.Pump(window);
            PrScreenshot.SaveWindowAtZoomLevels(window, $"category-{theme.Key.ToString()!.ToLowerInvariant()}");
        }

        // The trash can on a row only the later states set.
        tree.Project.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        window.GetVisualDescendants().OfType<VariableRemoveButton>()
            .First(button => button.InstanceMember?.Name == "TrackBackground.Blue").Remove();
        ScreenshotWindow.Pump(window);
        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            window.RequestedThemeVariant = theme;
            ScreenshotWindow.Pump(window);
            PrScreenshot.SaveWindowAtZoomLevels(window, $"after-trash-{theme.Key.ToString()!.ToLowerInvariant()}");
        }
    });
}
