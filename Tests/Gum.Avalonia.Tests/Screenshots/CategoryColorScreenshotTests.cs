using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Plugins.StateAnimation;
using Gum.Avalonia.Plugins.States;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.StateAnimation.SaveClasses;
using Microsoft.Extensions.DependencyInjection;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The States, Variables and Animations tabs over a component with several categories, in the real main
/// window, for the category color PR's before/after (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt;</c>). Run it on a
/// source tree with and without the feature; <see cref="ApplyColors"/> is what differs.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class CategoryColorScreenshotTests
{
    [SkippableFact]
    public void CategoryColorsAtZoomLevels() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = BuildProject(tree);
        ApplyColors(button);

        tree.Input.Window.Content = null;
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 1500;
        window.Height = 1100;
        window.Show();
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        Dispatcher.UIThread.RunJobs();

        void Reselect()
        {
            tree.SelectedState.SelectedElement = null;
            ScreenshotWindow.Pump(window);
            tree.SelectedState.SelectedElement = button;
            ScreenshotWindow.Pump(window);
        }
        StateTreeViewModel statesVm() => (StateTreeViewModel)window.GetVisualDescendants().OfType<AvaloniaStateTreeView>().Single().DataContext!;

        Reselect();
        CategoryViewModel size = statesVm().Categories.Single(c => c.Title == "Size");
        size.IsExpanded = true;
        statesVm().Categories.Single(c => c.Title == "Layout").IsSelected = true;
        ScreenshotWindow.Pump(window);

        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            window.RequestedThemeVariant = theme;
            ScreenshotWindow.Pump(window);
            string themeName = theme.Key.ToString()!.ToLowerInvariant();
            PrScreenshot.SaveWindowAtZoomLevels(window, $"states-{themeName}");
        }

        // Variables tab while editing a state of a colored category, scrolled to the rows it sets.
        Reselect();
        statesVm().Categories.Single(c => c.Title == "Size").IsExpanded = true;
        ScreenshotWindow.Pump(window);
        StateSave large = statesVm().Categories.Single(c => c.Title == "Size").States.Single(s => s.Title == "Large").Data;
        tree.SelectedState.SelectedStateSave = large;
        ScreenshotWindow.Pump(window);
        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            window.RequestedThemeVariant = theme;
            ScreenshotWindow.Pump(window);
            PrScreenshot.SaveWindowAtZoomLevels(window, $"variables-{theme.Key.ToString()!.ToLowerInvariant()}");
        }

        // Animations tab.
        Reselect();
        TabItem animationsTab = window.GetVisualDescendants().OfType<TabItem>().First(t => (t.DataContext as AvaloniaPluginTab)?.Title == "Animations");
        animationsTab.IsSelected = true;
        Grid rightGrid = animationsTab.GetVisualAncestors().OfType<Grid>().First(g => g.RowDefinitions.Count == 3);
        rightGrid.RowDefinitions[2].Height = new GridLength(520, GridUnitType.Pixel);
        ScreenshotWindow.Pump(window);
        AnimationsView animations = window.GetVisualDescendants().OfType<AnimationsView>().Single();
        ElementAnimationsViewModel animationsVm = (ElementAnimationsViewModel)animations.DataContext!;
        animationsVm.SelectedAnimation = animationsVm.Animations.Single(a => a.Name == "Grow");
        ScreenshotWindow.Pump(window);
        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            window.RequestedThemeVariant = theme;
            ScreenshotWindow.Pump(window);
            PrScreenshot.SaveWindowAtZoomLevels(window, $"animations-{theme.Key.ToString()!.ToLowerInvariant()}");
        }
    });

    /// <summary>Marks categories with colors. Empty on a tree that predates the feature.</summary>
    private static void ApplyColors(ComponentSave button)
    {
    }

    internal static ComponentSave BuildProject(ProjectTreeHarness tree)
    {
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Label", "Text");
        void Category(string name, params (string State, (string Var, object Value, string Type)[] Sets)[] states)
        {
            StateSaveCategory category = tree.Project.AddCategory(button, name);
            foreach (var (stateName, sets) in states)
            {
                StateSave state = tree.Project.AddState(button, category, stateName);
                foreach (var (variable, value, type) in sets)
                {
                    state.SetValue(variable, value, type);
                }
            }
        }
        Category("Size",
            ("Small", new[] { ("Width", (object)80f, "float"), ("Height", 24f, "float") }),
            ("Medium", new[] { ("Width", (object)120f, "float"), ("Height", 32f, "float") }),
            ("Large", new[] { ("Width", (object)200f, "float"), ("Height", 48f, "float") }));
        Category("Look",
            ("Normal", new[] { ("Alpha", (object)255, "int") }),
            ("Dim", new[] { ("Alpha", (object)128, "int") }));
        Category("Visibility",
            ("Shown", new[] { ("Visible", (object)true, "bool") }),
            ("Hidden", new[] { ("Visible", (object)false, "bool") }));
        Category("Layout",
            ("Compact", new[] { ("Width", (object)100f, "float"), ("X", 4f, "float") }),
            ("Wide", new[] { ("Width", (object)300f, "float"), ("X", 0f, "float") }));
        Category("Misc",
            ("A", new[] { ("Rotation", (object)0f, "float") }),
            ("B", new[] { ("Rotation", (object)15f, "float") }));

        ElementAnimationsSave animations = new ElementAnimationsSave { ElementName = "Button" };
        void Animation(string name, params (string State, float Time)[] frames)
        {
            AnimationSave animation = new AnimationSave { Name = name };
            foreach ((string stateName, float time) in frames)
            {
                animation.States.Add(new AnimatedStateSave { StateName = stateName, Time = time });
            }
            animations.Animations.Add(animation);
        }
        Animation("Grow", ("Size/Small", 0), ("Size/Large", 0.5f));
        Animation("Fade", ("Look/Normal", 0), ("Look/Dim", 0.4f));
        Animation("Blink", ("Misc/A", 0), ("Misc/B", 0.25f));
        tree.SaveAll();
        animations.Save(System.IO.Path.Combine(tree.Project.ProjectFolder, "Components", "ButtonAnimations.ganx"));
        // The Animations tab reads the sidecar on selection: select something else, then back.
        tree.Click(tree.NodeFor(button.Instances[0]));
        tree.Click(tree.NodeFor(button));
        return button;
    }
}
