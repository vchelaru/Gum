using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Shared base for tests that drive a Forms control built by the real file loader (a packaged theme's
/// .gucx components) with a scripted cursor and keyboard, the way the Gum Preview and a game loading a
/// project do. A theme project is loaded once per theme and reused: each test removes the screens the
/// previous test added instead of reloading the whole project.
/// </summary>
public abstract class FileThemeInputTestBase : BeginnerDriverTestBase
{
    private static readonly string[] ThemeNames =
        { "", "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95" };

    // Names that differ in the plain FormsTemplate (the "" theme).
    private static readonly Dictionary<string, string> TemplateNames = new Dictionary<string, string>
    {
        ["Button"] = "ButtonStandard",
        ["ColorPicker"] = "ColorPickerStandard",
        ["Expander"] = "ExpanderStandard",
        ["Splitter"] = "SplitterStandard",
    };

    private static readonly Dictionary<string, (GumProjectSave Project, int ScreenCount)> Loaded =
        new Dictionary<string, (GumProjectSave, int)>();

    protected FileThemeInputTestBase()
    {
        // Keyboard navigation (Tab, arrows) is opt-in in a game: Game1 adds its keyboard to this list.
        FrameworkElement.KeyboardsForUiControl.Add(Driver.Keyboard);
        // BaseTestClass trims the click keys to Enter; a game has Enter and Space.
        FrameworkElement.ClickCombos.Add(new KeyCombo { PushedKey = Keys.Space });
        // BaseTestClass.Dispose resets the registry, so a later test would find the shape defaults gone.
        Gum.Renderables.DefaultStrokedCircleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultFilledRectangleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultStrokedRectangleRenderable.RegisterRuntimeTypes();
    }

    public override void Dispose()
    {
        FrameworkElement.KeyboardsForUiControl.Remove(Driver.Keyboard);
        Gum.GumService.Default.ModalRoot.Children.Clear();
        base.Dispose();
    }

    /// <summary>
    /// Every theme: for behavior that depends on the template (hit-test targets, geometry, state wiring).
    /// </summary>
    public static IEnumerable<object[]> AllThemes() => ThemeNames.Select(theme => new object[] { theme });

    /// <summary>
    /// The plain template plus one theme with chrome instances: for control logic that is the same in every
    /// theme (double-click, disabled, right-click, key handling).
    /// </summary>
    public static IEnumerable<object[]> RepresentativeThemes() =>
        new[] { "", "Retro95" }.Select(theme => new object[] { theme });

    protected static string Label(string theme) => theme.Length == 0 ? "Standard" : theme;

    /// <summary>The theme's project, loaded once; screens added by an earlier test are removed.</summary>
    protected static GumProjectSave LoadTheme(string theme) => LoadCached(theme, prepare: null);

    /// <summary>Same as <see cref="LoadTheme(string)"/> with extra one-time setup for tests that need it.</summary>
    protected static GumProjectSave LoadCached(string theme, Action<GumProjectSave>? prepare)
    {
        if (!Loaded.TryGetValue(theme, out (GumProjectSave Project, int ScreenCount) entry))
        {
            string themeFolder = theme.Length == 0 ? "FormsTemplate" : Path.Combine("FormsThemes", theme);
            string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", themeFolder, "GumProject.gumx");
            GumProjectSave loaded = GumProjectSave.Load(gumx, out _)!;
            loaded.Initialize();
            ObjectFinder.Self.GumProjectSave = loaded;
            loaded.ApplyAllVariableReferences(); // GumService.Initialize does this before the first build
            prepare?.Invoke(loaded);
            entry = (loaded, loaded.Screens.Count);
            Loaded[theme] = entry;
        }
        while (entry.Project.Screens.Count > entry.ScreenCount)
        {
            entry.Project.Screens.RemoveAt(entry.Project.Screens.Count - 1);
        }
        ObjectFinder.Self.GumProjectSave = entry.Project;
        // A running game gets this registration from GumService.Initialize; BaseTestClass.Dispose clears it.
        ElementSaveExtensions.RegisterGueInstantiationType("Container", typeof(Gum.GueDeriving.ContainerRuntime));
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        return entry.Project;
    }

    private static string LocateRepoRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory != null && !File.Exists(Path.Combine(directory, "GumFull.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory ?? throw new InvalidOperationException("Repo root not found.");
    }

    protected static string ComponentName(GumProjectSave project, string controlName)
    {
        string name = project.Components.Any(item => item.Name.EndsWith("Controls/" + controlName))
            ? controlName
            : TemplateNames[controlName];
        return project.Components.Single(item => item.Name.EndsWith("Controls/" + name)).Name;
    }

    /// <summary>The component whose name ends in "/Controls/&lt;control&gt;" (FormsTemplate names some differently), or null.</summary>
    protected static ComponentSave? Find(GumProjectSave project, params string[] names)
    {
        foreach (string name in names)
        {
            ComponentSave? found = project.Components.FirstOrDefault(item => item.Name.EndsWith("Controls/" + name));
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    protected static string ComponentNamed(GumProjectSave project, string suffix) =>
        project.Components.Single(item => item.Name.EndsWith(suffix)).Name;

    /// <summary>
    /// Instantiates the theme's control as the only instance of a screen, laid out at the top left
    /// (the way the existing click tests place a control).
    /// </summary>
    protected T Place<T>(GumProjectSave project, string controlName, Action<ScreenSave, InstanceSave>? configure = null)
        where T : FrameworkElement
    {
        ScreenSave screen = new ScreenSave { Name = "AloneScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave instance = new InstanceSave { Name = "Box", BaseType = ComponentName(project, controlName), ParentContainer = screen };
        screen.Instances.Add(instance);
        configure?.Invoke(screen, instance);
        project.Screens.Add(screen);
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        return (T)((InteractiveGue)screenVisual.GetGraphicalUiElementByName("Box")!).FormsControlAsObject!;
    }

    /// <summary>Places the component alone on a screen (instance "Box") and returns its visual.</summary>
    protected InteractiveGue PlaceComponent(GumProjectSave project, ComponentSave component)
    {
        ScreenSave screen = new ScreenSave { Name = "AloneScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = component.Name, ParentContainer = screen });
        project.Screens.Add(screen);
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        return (InteractiveGue)screenVisual.GetGraphicalUiElementByName("Box")!;
    }

    /// <summary>Loads the theme and places the component named by the first suffix ("A|B") that exists, alone.</summary>
    protected T PlaceAlone<T>(string theme, string componentSuffix) where T : FrameworkElement
    {
        GumProjectSave project = LoadTheme(theme);
        string componentName = componentSuffix.Split('|').Select(s => project.Components.FirstOrDefault(item => item.Name.EndsWith(s))?.Name).First(n => n != null)!;
        return (T)PlaceComponent(project, project.Components.Single(item => item.Name == componentName)).FormsControlAsObject!;
    }

    /// <summary>Loads the theme's DemoScreenGum, adds it to the root and runs two frames.</summary>
    protected GraphicalUiElement LoadDemoScreen(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        ScreenSave demo = project.Screens.Single(item => item.Name.EndsWith("DemoScreenGum"));
        GraphicalUiElement screenVisual = demo.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        return screenVisual;
    }

    /// <summary>One keystroke with the modifiers held around it.</summary>
    protected void Press(Keys key, params Keys[] modifiers)
    {
        foreach (Keys modifier in modifiers) { Driver.SetKey(modifier, true); }
        Driver.PressKey(key);
        foreach (Keys modifier in modifiers) { Driver.SetKey(modifier, false); }
    }

    /// <summary>Every top-level Forms control under the element, in tree order.</summary>
    protected static void CollectControls(GraphicalUiElement element, List<FrameworkElement> into)
    {
        if (element is InteractiveGue gue && gue.FormsControlAsObject is FrameworkElement control)
        {
            into.Add(control);
        }
        foreach (GraphicalUiElement child in element.Children.OfType<GraphicalUiElement>())
        {
            CollectControls(child, into);
        }
    }

    protected static void Resize(InteractiveGue visual, float width, float height)
    {
        visual.WidthUnits = DimensionUnitType.Absolute;
        visual.HeightUnits = DimensionUnitType.Absolute;
        visual.Width = width;
        visual.Height = height;
    }

    protected static IEnumerable<GraphicalUiElement> Descendants(GraphicalUiElement root)
    {
        foreach (GraphicalUiElement child in root.Children.OfType<GraphicalUiElement>())
        {
            yield return child;
            foreach (GraphicalUiElement deeper in Descendants(child))
            {
                yield return deeper;
            }
        }
    }

    protected static string PathFrom(GraphicalUiElement element, GraphicalUiElement root)
    {
        List<string> names = new List<string>();
        for (GraphicalUiElement? current = element; current != null && current != root; current = current.Parent as GraphicalUiElement)
        {
            names.Add(current.Name ?? "?");
        }
        names.Reverse();
        return string.Join("/", names);
    }

    protected void DragFrom(int x, int y, int toX, int toY)
    {
        Driver.MoveTo(x, y);
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: true);
        for (int i = 1; i <= 4; i++)
        {
            Driver.MoveTo(x + (toX - x) * i / 4, y + (toY - y) * i / 4);
        }
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: false);
    }

    protected static int OffX => (int)GraphicalUiElement.CanvasWidth - 5;

    protected static int OffY => (int)GraphicalUiElement.CanvasHeight - 5;

    protected static int CenterX(GraphicalUiElement g) => (int)(g.AbsoluteLeft + g.AbsoluteWidth / 2);

    protected static int CenterY(GraphicalUiElement g) => (int)(g.AbsoluteTop + g.AbsoluteHeight / 2);

    protected void ClickCenter(GraphicalUiElement g, Gum.Automation.MouseButton button = Gum.Automation.MouseButton.Primary) =>
        Driver.Click(CenterX(g), CenterY(g), button);

    /// <summary>The state the visual last applied in a category (null when none was applied).</summary>
    protected static string? AppliedState(GraphicalUiElement visual, string category)
    {
        FieldInfo field = typeof(GraphicalUiElement).GetField("_appliedCategoryStates", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Dictionary<string, string>? applied = (Dictionary<string, string>?)field.GetValue(visual);
        return applied != null && applied.TryGetValue(category, out string? state) ? state : null;
    }

    protected static string Describe(InteractiveGue? hit)
    {
        if (hit == null)
        {
            return "nothing";
        }
        List<string> path = new List<string>();
        for (GraphicalUiElement? current = hit; current != null && path.Count < 4; current = current.Parent as GraphicalUiElement)
        {
            path.Add(current.Name ?? "?");
        }
        return string.Join(" < ", path);
    }
}
