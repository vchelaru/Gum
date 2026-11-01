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

namespace MonoGameGum.Tests.Forms.Themed;

/// <summary>Loads a packaged theme through the real loader and places controls alone on a screen.</summary>
internal static class ThemedControlHarness
{
    public static readonly string[] ThemeNames =
        { "", "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95" };

    public static IEnumerable<object[]> Themes() => ThemeNames.Select(theme => new object[] { theme });

    public static GumProjectSave LoadTheme(string theme)
    {
        // BaseTestClass.Dispose resets the registry, so register the shape defaults per test.
        Gum.Renderables.DefaultStrokedCircleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultFilledRectangleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultStrokedRectangleRenderable.RegisterRuntimeTypes();
        string themeFolder = theme.Length == 0 ? "FormsTemplate" : Path.Combine("FormsThemes", theme);
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", themeFolder, "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        project.ApplyAllVariableReferences(); // GumService.Initialize does this before the first build
        ElementSaveExtensions.RegisterGueInstantiationType("Container", typeof(Gum.GueDeriving.ContainerRuntime));
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        return project;
    }

    /// <summary>The component whose name ends in "/Controls/&lt;control&gt;" (FormsTemplate names some differently), or null.</summary>
    public static ComponentSave? Find(GumProjectSave project, params string[] names)
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

    public static string ScrollBarName => "ScrollBar";

    public static InteractiveGue PlaceAlone(Gum.Automation.GumDriver driver, GumProjectSave project, ComponentSave component)
    {
        ScreenSave screen = new ScreenSave { Name = "AloneScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = component.Name, ParentContainer = screen });
        project.Screens.Add(screen);
        project.Initialize();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        driver.Step(2);
        return (InteractiveGue)screenVisual.GetGraphicalUiElementByName("Box")!;
    }

    public static void Resize(InteractiveGue visual, float width, float height)
    {
        visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        visual.Width = width;
        visual.Height = height;
    }

    public static IEnumerable<GraphicalUiElement> Descendants(GraphicalUiElement root)
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

    public static string Path_(GraphicalUiElement element, GraphicalUiElement root)
    {
        List<string> names = new List<string>();
        for (GraphicalUiElement? current = element; current != null && current != root; current = current.Parent as GraphicalUiElement)
        {
            names.Add(current.Name ?? "?");
        }
        names.Reverse();
        return string.Join("/", names);
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
