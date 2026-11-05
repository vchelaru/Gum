using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Gum.Avalonia.Services;
using Gum.Services;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Shouldly;
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

        // The States tab's right-click menu on a colored category, with the Color submenu open.
        Reselect();
        statesVm().Categories.Single(c => c.Title == "Size").IsExpanded = true;
        TreeView treeView = window.GetVisualDescendants().OfType<AvaloniaStateTreeView>().Single().GetVisualDescendants().OfType<TreeView>().First();
        ContextMenu menu = treeView.ContextMenu!;
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window, scale);
        double originalSize = scale.BaseFontSize;
        try
        {
            foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                window.RequestedThemeVariant = theme;
                string themeName = theme.Key.ToString()!.ToLowerInvariant();
                foreach (double zoom in PrScreenshot.ZoomLevels)
                {
                    scale.BaseFontSize = zoom;
                    ScreenshotWindow.Pump(window);
                    statesVm().Categories.Single(c => c.Title == "Layout").IsSelected = true;
                    ScreenshotWindow.Pump(window);
                    Control row = window.GetVisualDescendants().OfType<TreeViewItem>().First(t => (t.DataContext as StateTreeViewItem)?.Title == "Layout");
                    Point at = row.TranslatePoint(new Point(row.Bounds.Width * 0.4, row.Bounds.Height / 2), window)!.Value;
                    window.MouseMove(at);
                    window.MouseDown(at, MouseButton.Right);
                    window.MouseUp(at, MouseButton.Right);
                    for (int i = 0; i < 100 && !menu.IsOpen; i++)
                    {
                        Thread.Sleep(20);
                        ScreenshotWindow.Pump(window);
                    }
                    menu.IsOpen.ShouldBeTrue("The category right-click did not open the menu.");
                    menu.Items.OfType<MenuItem>().Single(item => item.Header as string == "Color").IsSubMenuOpen = true;
                    ScreenshotWindow.Pump(window);
                    Thread.Sleep(100);
                    PrScreenshot.SaveWindow(window, $"menu-{themeName}-zoom-{zoom}");
                    menu.Close();
                    ScreenshotWindow.Pump(window);
                }
            }
        }
        finally
        {
            scale.BaseFontSize = originalSize;
        }
    });

    /// <summary>
    /// One contact sheet per theme (<c>palette-contact-dark.png</c>, <c>palette-contact-light.png</c>): a
    /// component with one category per palette color, so the States tab shows every category mark, with the
    /// Color submenu open; one tile per zoom level.
    /// </summary>
    [SkippableFact]
    public void PaletteContactSheets() => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave palette = tree.Project.AddComponent("Palette");
        foreach (string colorName in Gum.Plugins.InternalPlugins.StatePlugin.CategoryColors.Names)
        {
            StateSaveCategory category = tree.Project.AddCategory(palette, colorName + "Cat");
            StateSave state = tree.Project.AddState(palette, category, "On");
            state.SetValue("Width", 100f, "float");
            category.ColorName = colorName;
        }
        tree.SaveAll();

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
        tree.SelectedState.SelectedElement = null;
        ScreenshotWindow.Pump(window);
        tree.SelectedState.SelectedElement = palette;
        ScreenshotWindow.Pump(window);

        StateTreeViewModel statesVm = (StateTreeViewModel)window.GetVisualDescendants().OfType<AvaloniaStateTreeView>().Single().DataContext!;
        TreeView treeView = window.GetVisualDescendants().OfType<AvaloniaStateTreeView>().Single().GetVisualDescendants().OfType<TreeView>().First();
        ContextMenu menu = treeView.ContextMenu!;
        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            window.RequestedThemeVariant = theme;
            ScreenshotWindow.Pump(window);
            PrScreenshot.SaveContactSheet(window, $"palette-contact-{theme.Key.ToString()!.ToLowerInvariant()}",
                prepare: _ =>
                {
                    statesVm.Categories.Single(c => c.Title == "TealCat").IsSelected = true;
                    ScreenshotWindow.Pump(window);
                    Control row = window.GetVisualDescendants().OfType<TreeViewItem>().First(t => (t.DataContext as StateTreeViewItem)?.Title == "TealCat");
                    Point at = row.TranslatePoint(new Point(row.Bounds.Width * 0.4, row.Bounds.Height / 2), window)!.Value;
                    window.MouseMove(at);
                    window.MouseDown(at, MouseButton.Right);
                    window.MouseUp(at, MouseButton.Right);
                    for (int i = 0; i < 100 && !menu.IsOpen; i++)
                    {
                        Thread.Sleep(20);
                        ScreenshotWindow.Pump(window);
                    }
                    menu.IsOpen.ShouldBeTrue("The category right-click did not open the menu.");
                    menu.Items.OfType<MenuItem>().Single(item => item.Header as string == "Color").IsSubMenuOpen = true;
                    ScreenshotWindow.Pump(window);
                    Thread.Sleep(100);
                },
                cleanup: () =>
                {
                    menu.Close();
                    ScreenshotWindow.Pump(window);
                });
        }
    });

    /// <summary>Marks four of the five categories with colors; Misc stays uncolored.</summary>
    private static void ApplyColors(ComponentSave button)
    {
        foreach ((string name, string color) in new[] { ("Size", "Indigo"), ("Look", "Magenta"), ("Visibility", "Teal"), ("Layout", "Brown") })
        {
            button.Categories.Single(category => category.Name == name).ColorName = color;
        }
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
