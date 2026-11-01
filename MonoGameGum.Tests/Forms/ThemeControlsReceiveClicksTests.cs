using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Clicking the center of any Forms control of any packaged theme reaches that control (#7424). A
/// chrome child with the default HasEvents = true (a Container-based component instance covering
/// the control) takes the hit instead, so the control never sees Click, Push or focus.
/// </summary>
/// <remarks>
/// A hit lands on the control when Cursor.VisualOver is the control's own visual, a nested Forms control, or
/// a part the control wires up itself (WiredParts). Any other descendant is chrome that swallows the click.
/// Only top-level controls are clicked, not controls nested inside another Forms control. Excluded: Label,
/// StackPanel, Panel and UserControl (layout only, no events by design), disabled controls, controls with
/// no area and controls whose center is off the canvas.
/// </remarks>
public class ThemeControlsReceiveClicksTests : BeginnerDriverTestBase
{
    private static readonly string[] NoEventControls = { "Label", "StackPanel", "Panel", "UserControl" };

    // Forms controls that only hold other controls; a control inside one is still a top-level click target.
    private static readonly string[] HoldingControls = { "Panel", "StackPanel", "UserControl", "Window", "DialogBox", "ScrollViewer", "Toast" };

    // Child visuals the Forms control subscribes to itself (Slider and ScrollBar track, Expander header,
    // ColorPicker areas, ItemsControl scroll area); a hit there reaches the control through its own handlers.
    private static readonly HashSet<string> WiredParts = new HashSet<string>
    {
        "TrackInstance", "ThumbInstance", "HeaderContainer", "SaturationValueContainer", "HueContainer",
        "ClipAndScrollContainer", "ClipContainer", "InnerPanelInstance",
    };

    public ThemeControlsReceiveClicksTests()
    {
        // BaseTestClass.Dispose resets the registry, so a later test would find the shape defaults gone.
        Gum.Renderables.DefaultStrokedCircleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultFilledRectangleRenderable.RegisterRuntimeTypes();
        Gum.Renderables.DefaultStrokedRectangleRenderable.RegisterRuntimeTypes();
    }

    public static IEnumerable<object[]> Themes() =>
        new[] { "", "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95" }
            .Select(theme => new object[] { theme });

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickCenter_EveryControlInDemoScreen_HitsTheControl(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        ScreenSave demo = project.Screens.Single(item => item.Name.EndsWith("DemoScreenGum"));

        GraphicalUiElement screenVisual = demo.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);

        List<string> failures = ClickEveryControl(screenVisual, $"{theme} DemoScreenGum", out int clicked);

        clicked.ShouldBeGreaterThan(5);
        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickCenter_EveryControlAlone_HitsTheControl(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        List<string> controls = project.Components
            .Select(item => item.Name)
            .Where(name => name.Contains("Controls/") && !NoEventControls.Any(skip => name.EndsWith("/" + skip)))
            .ToList();
        List<string> failures = new List<string>();
        int clicked = 0;

        foreach (string componentName in controls)
        {
            ScreenSave screen = new ScreenSave { Name = "AloneScreen" };
            screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
            screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = componentName, ParentContainer = screen });
            project.Screens.Add(screen);
            project.Initialize();

            GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
            screenVisual.AddToRoot();
            Driver.Step(2);

            failures.AddRange(ClickEveryControl(screenVisual, $"{theme} {componentName} alone", out int count));
            clicked += count;

            screenVisual.RemoveFromRoot();
            project.Screens.Remove(screen);
        }

        clicked.ShouldBeGreaterThan(10);
        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }

    private List<string> ClickEveryControl(GraphicalUiElement root, string scene, out int clicked)
    {
        List<InteractiveGue> targets = new List<InteractiveGue>();
        CollectControls(root, targets);
        List<string> failures = new List<string>();
        clicked = 0;

        foreach (InteractiveGue visual in targets)
        {
            FrameworkElement control = (FrameworkElement)visual.FormsControlAsObject!;
            string typeName = control.GetType().Name;
            if (IsNestedInControl(visual))
            {
                continue;
            }

            if (typeName == "Label" || typeName == "Panel" || !control.IsEnabled || !visual.AbsoluteVisible
                || visual.GetAbsoluteWidth() < 1f || visual.GetAbsoluteHeight() < 1f)
            {
                continue;
            }

            int x = (int)(visual.AbsoluteLeft + visual.GetAbsoluteWidth() / 2);
            int y = (int)(visual.AbsoluteTop + visual.GetAbsoluteHeight() / 2);
            if (x < 0 || y < 0 || x >= GraphicalUiElement.CanvasWidth || y >= GraphicalUiElement.CanvasHeight)
            {
                continue;
            }
            Driver.MoveTo(x, y);
            clicked++;

            InteractiveGue? hit = Driver.Cursor.VisualOver;
            if (!LandsOnControl(visual, hit))
            {
                failures.Add($"{scene}: {typeName} '{visual.Name}' center ({x},{y}) hit {Describe(hit)}");
            }
        }

        return failures;
    }

    private static bool IsNestedInControl(InteractiveGue visual)
    {
        for (GraphicalUiElement? current = visual.Parent as GraphicalUiElement; current != null; current = current.Parent as GraphicalUiElement)
        {
            if (current is InteractiveGue gue && gue.FormsControlAsObject is FrameworkElement owner
                && !HoldingControls.Contains(owner.GetType().Name))
            {
                return true;
            }
        }
        return false;
    }

    private static bool LandsOnControl(InteractiveGue control, InteractiveGue? hit)
    {
        if (hit == null)
        {
            return false;
        }
        for (GraphicalUiElement? current = hit; current != null; current = current.Parent as GraphicalUiElement)
        {
            if (current == control)
            {
                // The control itself, a nested Forms control, or a part the control wires up itself.
                return hit == control || hit.FormsControlAsObject != null || WiredParts.Contains(hit.Name ?? "");
            }
        }
        return false;
    }

    private static string Describe(InteractiveGue? hit)
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

    private static void CollectControls(GraphicalUiElement element, List<InteractiveGue> into)
    {
        if (element is InteractiveGue gue && gue.FormsControlAsObject is FrameworkElement)
        {
            into.Add(gue);
        }
        foreach (GraphicalUiElement child in element.Children.OfType<GraphicalUiElement>())
        {
            CollectControls(child, into);
        }
    }

    private static GumProjectSave LoadTheme(string theme)
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
}
