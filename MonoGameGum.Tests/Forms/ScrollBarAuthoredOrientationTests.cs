using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The orientation of a ScrollBar authored in the Gum tool: what the control and its visual
/// show for a ScrollBar placed in a screen, and that code can still assign the orientation.
/// </summary>
public class ScrollBarAuthoredOrientationTests : BaseTestClass
{
    [Fact]
    public void ToolAuthoredScrollBar_WithNothingAuthored_ShouldBeVertical()
    {
        GraphicalUiElement screen = CreateScreenWithScrollBar(authoredVariables: Array.Empty<VariableSave>());

        ScrollBar scrollBar = GetScrollBar(screen);

        scrollBar.Orientation.ShouldBe(Orientation.Vertical);
        VisualLooksHorizontal(scrollBar).ShouldBeFalse();
    }

    [Fact]
    public void ToolAuthoredScrollBar_WithOrientationCategoryStateHorizontal_ShouldLookHorizontal()
    {
        VariableSave stateVariable = new VariableSave
        {
            Name = "ScrollBarInstance.OrientationCategoryState",
            Type = "OrientationCategory",
            Value = "Horizontal",
            SetsValue = true,
        };

        GraphicalUiElement screen = CreateScreenWithScrollBar(new[] { stateVariable });

        ScrollBar scrollBar = GetScrollBar(screen);
        VisualLooksHorizontal(scrollBar).ShouldBeTrue();
        scrollBar.Orientation.ShouldBe(Orientation.Horizontal);
    }

    [Fact]
    public void ToolAuthoredScrollBar_AssigningOrientationInCode_ShouldSwitchControlAndVisual()
    {
        GraphicalUiElement screen = CreateScreenWithScrollBar(authoredVariables: Array.Empty<VariableSave>());
        ScrollBar scrollBar = GetScrollBar(screen);

        scrollBar.Orientation = Orientation.Horizontal;

        scrollBar.Orientation.ShouldBe(Orientation.Horizontal);
        VisualLooksHorizontal(scrollBar).ShouldBeTrue();

        scrollBar.Orientation = Orientation.Vertical;

        scrollBar.Orientation.ShouldBe(Orientation.Vertical);
        VisualLooksHorizontal(scrollBar).ShouldBeFalse();
    }

    [Fact]
    public void ToolAuthoredScrollBar_OrientationAssignedBeforeSwappingTheVisual_ShouldKeepTheAssignedOrientation()
    {
        GraphicalUiElement screen = CreateScreenWithScrollBar(authoredVariables: Array.Empty<VariableSave>());
        ScrollBar scrollBar = GetScrollBar(screen);
        scrollBar.Orientation = Orientation.Horizontal;
        GraphicalUiElement replacement = LoadedProject.Components
            .Single(item => item.Name == "Controls/ScrollBar").ToGraphicalUiElement();
        ((InteractiveGue)replacement).FormsControlAsObject = null;

        scrollBar.Visual = (InteractiveGue)replacement;

        scrollBar.Orientation.ShouldBe(Orientation.Horizontal);
        VisualLooksHorizontal(scrollBar).ShouldBeTrue();
    }

    [Fact]
    public void ToolAuthoredScrollBar_WithOrientationAuthoredHorizontal_ShouldFollowOnControlAndVisual()
    {
        VariableSave orientation = new VariableSave
        {
            Name = "ScrollBarInstance.Orientation",
            Type = "Orientation",
            Value = Orientation.Horizontal,
            SetsValue = true,
        };

        GraphicalUiElement screen = CreateScreenWithScrollBar(new[] { orientation });

        ScrollBar scrollBar = GetScrollBar(screen);
        scrollBar.Orientation.ShouldBe(Orientation.Horizontal);
        VisualLooksHorizontal(scrollBar).ShouldBeTrue();
    }

    [Fact]
    public void ToolAuthoredScrollBar_OrientationAuthoredHorizontal_CodeAssignedVerticalShouldWin()
    {
        VariableSave orientation = new VariableSave
        {
            Name = "ScrollBarInstance.Orientation",
            Type = "Orientation",
            Value = Orientation.Horizontal,
            SetsValue = true,
        };
        GraphicalUiElement screen = CreateScreenWithScrollBar(new[] { orientation });
        ScrollBar scrollBar = GetScrollBar(screen);

        scrollBar.Orientation = Orientation.Vertical;

        scrollBar.Orientation.ShouldBe(Orientation.Vertical);
        VisualLooksHorizontal(scrollBar).ShouldBeFalse();
    }

    [Fact]
    public void ToolAuthoredScrollViewer_HorizontalScrollBar_ShouldBeHorizontalOnControlAndVisual()
    {
        GraphicalUiElement screen = CreateScreen("ScrollViewerInstance", "Controls/ScrollViewer", Array.Empty<VariableSave>());

        InteractiveGue horizontalVisual = (InteractiveGue)screen
            .GetGraphicalUiElementByName("ScrollViewerInstance")!
            .GetGraphicalUiElementByName("HorizontalScrollBarInstance")!;
        ScrollBar horizontalScrollBar = (ScrollBar)horizontalVisual.FormsControlAsObject;

        horizontalScrollBar.Orientation.ShouldBe(Orientation.Horizontal);
        VisualLooksHorizontal(horizontalScrollBar).ShouldBeTrue();
    }

    private static GumProjectSave LoadedProject => ObjectFinder.Self.GumProjectSave!;

    private GraphicalUiElement CreateScreenWithScrollBar(VariableSave[] authoredVariables) =>
        CreateScreen("ScrollBarInstance", "Controls/ScrollBar", authoredVariables);

    private GraphicalUiElement CreateScreen(string instanceName, string instanceType, VariableSave[] authoredVariables)
    {
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsTemplate", "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        ScreenSave screen = new ScreenSave { Name = "ScrollBarScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        screen.Instances.Add(new InstanceSave { Name = instanceName, BaseType = instanceType, ParentContainer = screen });
        foreach (VariableSave variable in authoredVariables)
        {
            defaultState.Variables.Add(variable);
        }
        project.Screens.Add(screen);
        project.Initialize();

        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.UpdateLayout();
        return screenVisual;
    }

    private static ScrollBar GetScrollBar(GraphicalUiElement screen) =>
        (ScrollBar)((InteractiveGue)screen.GetGraphicalUiElementByName("ScrollBarInstance")!).FormsControlAsObject;

    /// <summary>The Horizontal state gives the bar an absolute 24 height; the Vertical state fills its parent.</summary>
    private static bool VisualLooksHorizontal(ScrollBar scrollBar)
    {
        scrollBar.Visual.UpdateLayout();
        return scrollBar.Visual.HeightUnits == Gum.DataTypes.DimensionUnitType.Absolute;
    }

    private static string LocateRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumFull.sln")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not locate GumFull.sln by walking up from {AppContext.BaseDirectory}.");
    }
}
