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
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Base for tests that drive keyboard and gamepad input through screens and components loaded from the
/// packaged themes by the real file loader (not code-built visuals).
/// </summary>
public abstract class ThemeKeyboardTestBase : BeginnerDriverTestBase
{
    protected ThemeKeyboardTestBase()
    {
        // Keyboard navigation (Tab, arrows) is opt-in in a game: Game1 adds its keyboard to this list.
        FrameworkElement.KeyboardsForUiControl.Add(Driver.Keyboard);
        // BaseTestClass.Dispose resets the registry, so a later test would find the shape defaults gone.
        Gum.Renderables.DefaultStrokedCircleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultFilledRectangleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultStrokedRectangleRenderable.RegisterRuntimeTypes();
    }

    public override void Dispose()
    {
        FrameworkElement.KeyboardsForUiControl.Remove(Driver.Keyboard);
        base.Dispose();
    }

    public static IEnumerable<object[]> Themes() =>
        new[] { "", "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95" }
            .Select(theme => new object[] { theme });

    protected static string Label(string theme) => theme.Length == 0 ? "Standard" : theme;

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

    protected static string ComponentNamed(GumProjectSave project, string suffix) =>
        project.Components.Single(item => item.Name.EndsWith(suffix)).Name;

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

    /// <summary>
    /// Places the component alone on a screen (instance named "Box") loaded from the theme, so the control
    /// is built by the real loader. Returns the Forms control.
    /// </summary>
    protected T PlaceAlone<T>(string theme, string componentSuffix) where T : FrameworkElement
    {
        GumProjectSave project = LoadTheme(theme);
        string componentName = componentSuffix.Split('|').Select(s => project.Components.FirstOrDefault(item => item.Name.EndsWith(s))?.Name).First(n => n != null)!;
        ScreenSave screen = new ScreenSave { Name = "AloneScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = componentName, ParentContainer = screen });
        project.Screens.Add(screen);
        project.Initialize();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        InteractiveGue box = (InteractiveGue)screenVisual.GetGraphicalUiElementByName("Box")!;
        return (T)box.FormsControlAsObject!;
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

    private static string LocateRepoRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory != null && !File.Exists(Path.Combine(directory, "GumFull.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory ?? throw new InvalidOperationException("Repo root not found.");
    }
}
