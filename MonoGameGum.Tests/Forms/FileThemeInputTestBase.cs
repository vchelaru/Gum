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

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Base for tests that drive a Forms control built by the real file loader (a packaged theme's .gucx
/// components) with a scripted cursor, the way the Gum Preview and a game loading a project do.
/// </summary>
public abstract class FileThemeInputTestBase : BeginnerDriverTestBase
{
    // Names that differ in the plain FormsTemplate (the "" theme).
    private static readonly Dictionary<string, string> TemplateNames = new Dictionary<string, string>
    {
        ["Button"] = "ButtonStandard",
        ["ColorPicker"] = "ColorPickerStandard",
        ["Expander"] = "ExpanderStandard",
        ["Splitter"] = "SplitterStandard",
    };

    protected FileThemeInputTestBase()
    {
        // BaseTestClass.Dispose resets the registry, so a later test would find the shape defaults gone.
        Gum.Renderables.DefaultStrokedCircleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultFilledRectangleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultStrokedRectangleRenderable.RegisterRuntimeTypes();
    }

    public static IEnumerable<object[]> Themes() =>
        new[] { "", "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95" }
            .Select(theme => new object[] { theme });

    protected static GumProjectSave LoadTheme(string theme)
    {
        string themeFolder = theme.Length == 0 ? "FormsTemplate" : Path.Combine("FormsThemes", theme);
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", themeFolder, "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        // A running game gets this registration from GumService.Initialize.
        ElementSaveExtensions.RegisterGueInstantiationType("Container", typeof(Gum.GueDeriving.ContainerRuntime));
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        return project;
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
        project.Initialize();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        return (T)((InteractiveGue)screenVisual.GetGraphicalUiElementByName("Box")!).FormsControlAsObject!;
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
