using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// ScrollBar and ScrollViewer give LargeChange and SmallChange computed values when a Visual is
/// assigned. A value authored on the Visual (or set in code) replaces that computation and survives
/// a Visual swap. See #6255.
/// </summary>
public class LargeChangeAuthoredTests : BaseTestClass
{
    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    #region Viewport computation (unauthored)

    [Fact]
    public void ScrollBarNew_LargeChangeNotSet_FollowsViewportSize()
    {
        ScrollBar scrollBar = new();

        scrollBar.ViewportSize.ShouldBeGreaterThan(0);
        scrollBar.LargeChange.ShouldBe(scrollBar.ViewportSize);
    }

    [Fact]
    public void ScrollBarVisualSwap_LargeChangeNotSet_RecomputesFromViewportSize()
    {
        ScrollBar scrollBar = new();
        ScrollBarVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);

        scrollBar.Visual = newVisual;

        scrollBar.LargeChange.ShouldBe(scrollBar.ViewportSize);
    }

    [Fact]
    public void ScrollViewerNew_LargeChangeNotSet_InnerBarUsesViewportBasedValueNotTen()
    {
        ScrollViewer scrollViewer = new();

        scrollViewer.LargeChange.ShouldBeGreaterThan(10);
    }

    #endregion

    #region Set in code

    [Fact]
    public void ScrollBarVisualSwap_LargeChangeSetInCode_KeepsValue()
    {
        ScrollBar scrollBar = new();
        scrollBar.LargeChange = 25;
        ScrollBarVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);

        scrollBar.Visual = newVisual;

        scrollBar.LargeChange.ShouldBe(25);
    }

    [Fact]
    public void ScrollViewerVisualSwap_LargeChangeSetInCode_InnerBarsKeepValue()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.LargeChange = 25;
        ScrollViewerVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);

        scrollViewer.Visual = newVisual;

        scrollViewer.LargeChange.ShouldBe(25);
        GetVerticalBar(scrollViewer).LargeChange.ShouldBe(25);
    }

    [Fact]
    public void ScrollViewerSetLargeChange_BeforeInnerBarsExist_AppliesWhenVisualAssigned()
    {
        ScrollViewer scrollViewer = new(new Gum.GueDeriving.ContainerRuntime());
        scrollViewer.LargeChange = 25;
        ScrollViewerVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);

        scrollViewer.Visual = newVisual;

        GetVerticalBar(scrollViewer).LargeChange.ShouldBe(25);
    }

    #endregion

    #region Authored through the behavior applier

    [Fact]
    public void Apply_LargeChangeDeclaredWithoutValue_NotAuthored_LeavesViewportLargeChange()
    {
        ScrollBarVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        ScrollBar scrollBar = new(visual);
        double viewportLargeChange = scrollBar.LargeChange;
        AuthorBehavior(visual, new List<(string, object?)> { ("LargeChange", null) });

        BehaviorFormsPropertyApplier.Apply(scrollBar, visual);

        scrollBar.LargeChange.ShouldBe(viewportLargeChange);
    }

    [Fact]
    public void Apply_LargeChangeDeclaredWithValueTen_SetsTen()
    {
        ScrollBarVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        ScrollBar scrollBar = new(visual);
        AuthorBehavior(visual, new List<(string, object?)> { ("LargeChange", null) }, declaredDefault: 10.0);

        BehaviorFormsPropertyApplier.Apply(scrollBar, visual);

        scrollBar.LargeChange.ShouldBe(10);
    }

    [Fact]
    public void Apply_LargeChangeAuthored_SetsValueAndSurvivesVisualSwap()
    {
        ScrollBarVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        ScrollBar scrollBar = new(visual);
        AuthorBehavior(visual, new List<(string, object?)> { ("LargeChange", 25.0) });

        BehaviorFormsPropertyApplier.Apply(scrollBar, visual);
        scrollBar.LargeChange.ShouldBe(25);
        scrollBar.Visual = new ScrollBarVisual(fullInstantiation: true, tryCreateFormsObject: false);

        scrollBar.LargeChange.ShouldBe(25);
    }

    [Fact]
    public void Apply_ScrollViewerLargeChangeAuthored_FansToInnerBarsAndSurvivesVisualSwap()
    {
        ScrollViewerVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        ScrollViewer scrollViewer = new(visual);
        AuthorBehavior(visual, new List<(string, object?)> { ("LargeChange", 25.0) },
            behaviorName: StandardFormsBehaviorNames.ScrollViewerBehaviorName);

        BehaviorFormsPropertyApplier.Apply(scrollViewer, visual);
        GetVerticalBar(scrollViewer).LargeChange.ShouldBe(25);
        scrollViewer.Visual = new ScrollViewerVisual(fullInstantiation: true, tryCreateFormsObject: false);

        GetVerticalBar(scrollViewer).LargeChange.ShouldBe(25);
    }

    #endregion


    #region SmallChange

    [Fact]
    public void ScrollBarNew_SmallChangeNotSet_IsTen()
    {
        ScrollBar scrollBar = new();

        scrollBar.SmallChange.ShouldBe(10);
    }

    [Fact]
    public void ScrollBarWithoutTrackVisual_SmallChangeNotSet_FallsBackToTwo()
    {
        ScrollBar scrollBar = new(new Gum.GueDeriving.ContainerRuntime());

        scrollBar.SmallChange.ShouldBe(2);
    }

    [Fact]
    public void ScrollViewerNew_SmallChangeNotSet_InnerBarIsTen()
    {
        ScrollViewer scrollViewer = new();

        GetVerticalBar(scrollViewer).SmallChange.ShouldBe(10);
    }

    [Fact]
    public void Apply_SmallChangeDeclaredWithDefaultTen_SetsTen()
    {
        ScrollBarVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        ScrollBar scrollBar = new(visual);
        AuthorBehavior(visual, new List<(string, object?)> { ("SmallChange", null) }, declaredDefault: 10.0);

        BehaviorFormsPropertyApplier.Apply(scrollBar, visual);

        scrollBar.SmallChange.ShouldBe(10);
    }

    [Fact]
    public void ScrollBarVisualSwap_SmallChangeSetInCode_KeepsValue()
    {
        ScrollBar scrollBar = new();
        scrollBar.SmallChange = 7;

        scrollBar.Visual = new Gum.GueDeriving.ContainerRuntime();

        scrollBar.SmallChange.ShouldBe(7);
    }

    [Fact]
    public void ScrollViewerVisualSwap_SmallChangeSetInCode_InnerBarsKeepValue()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.SmallChange = 7;

        scrollViewer.Visual = new ScrollViewerVisual(fullInstantiation: true, tryCreateFormsObject: false);

        scrollViewer.SmallChange.ShouldBe(7);
        GetVerticalBar(scrollViewer).SmallChange.ShouldBe(7);
    }

    [Fact]
    public void ScrollViewerSetSmallChange_BeforeInnerBarsExist_AppliesWhenVisualAssigned()
    {
        ScrollViewer scrollViewer = new(new Gum.GueDeriving.ContainerRuntime());
        scrollViewer.SmallChange = 7;

        scrollViewer.Visual = new ScrollViewerVisual(fullInstantiation: true, tryCreateFormsObject: false);

        GetVerticalBar(scrollViewer).SmallChange.ShouldBe(7);
    }

    [Fact]
    public void Apply_SmallChangeAuthored_SetsValueAndSurvivesVisualSwap()
    {
        ScrollBarVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        ScrollBar scrollBar = new(visual);
        AuthorBehavior(visual, new List<(string, object?)> { ("SmallChange", 7.0) }, declaredDefault: 10.0);

        BehaviorFormsPropertyApplier.Apply(scrollBar, visual);
        scrollBar.SmallChange.ShouldBe(7);
        scrollBar.Visual = new Gum.GueDeriving.ContainerRuntime();

        scrollBar.SmallChange.ShouldBe(7);
    }

    [Fact]
    public void Apply_ScrollViewerSmallChangeAuthored_FansToInnerBarsAndSurvivesVisualSwap()
    {
        ScrollViewerVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        ScrollViewer scrollViewer = new(visual);
        AuthorBehavior(visual, new List<(string, object?)> { ("SmallChange", 7.0) }, declaredDefault: 10.0,
            behaviorName: StandardFormsBehaviorNames.ScrollViewerBehaviorName);

        BehaviorFormsPropertyApplier.Apply(scrollViewer, visual);
        GetVerticalBar(scrollViewer).SmallChange.ShouldBe(7);
        scrollViewer.Visual = new ScrollViewerVisual(fullInstantiation: true, tryCreateFormsObject: false);

        GetVerticalBar(scrollViewer).SmallChange.ShouldBe(7);
    }

    #endregion

    #region Swapping to a Visual without scroll bars

    [Fact]
    public void ScrollViewer_WithBars_ReadsLargeAndSmallChangeFromBar()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.LargeChange = 25;
        scrollViewer.SmallChange = 7;

        scrollViewer.LargeChange.ShouldBe(GetVerticalBar(scrollViewer).LargeChange);
        scrollViewer.SmallChange.ShouldBe(GetVerticalBar(scrollViewer).SmallChange);
        scrollViewer.LargeChange.ShouldBe(25);
        scrollViewer.SmallChange.ShouldBe(7);
    }

    [Fact]
    public void ScrollViewerVisualSwap_ToVisualWithoutBars_LargeChangeDoesNotReadRemovedBar()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.LargeChange = 25;

        scrollViewer.Visual = new Gum.GueDeriving.ContainerRuntime();

        scrollViewer.LargeChange.ShouldBe(0);
    }

    [Fact]
    public void ScrollViewerVisualSwap_ToVisualWithoutBars_SmallChangeDoesNotReadRemovedBar()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.SmallChange = 7;

        scrollViewer.Visual = new Gum.GueDeriving.ContainerRuntime();

        scrollViewer.SmallChange.ShouldBe(0);
    }

    [Fact]
    public void ScrollViewerVisualSwap_ToVisualWithoutBars_ClearsBarReferences()
    {
        ScrollViewer scrollViewer = new();

        scrollViewer.Visual = new Gum.GueDeriving.ContainerRuntime();

        scrollViewer.VerticalScrollBar.ShouldBeNull();
    }

    [Fact]
    public void ScrollViewerVisualSwap_ToVisualWithoutBarsAndBack_ReadsNewBar()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.LargeChange = 25;
        scrollViewer.Visual = new Gum.GueDeriving.ContainerRuntime();

        scrollViewer.Visual = new ScrollViewerVisual(fullInstantiation: true, tryCreateFormsObject: false);

        scrollViewer.LargeChange.ShouldBe(25);
        scrollViewer.VerticalScrollBar.ShouldBe(GetVerticalBar(scrollViewer));
    }

    #endregion

    #region Inner bar values set before the ScrollViewer wraps it

    [Fact]
    public void ScrollViewerAssign_InnerBarWithChangesSetInCode_KeepsThem()
    {
        ScrollViewerVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        ScrollBar innerBar = (ScrollBar)((InteractiveGue)visual.VerticalScrollBarInstance).FormsControlAsObject!;
        innerBar.SmallChange = 3;
        innerBar.LargeChange = 40;

        ScrollViewer scrollViewer = new(visual);

        innerBar.SmallChange.ShouldBe(3);
        innerBar.LargeChange.ShouldBe(40);
    }

    #endregion

    private static ScrollBar GetVerticalBar(ScrollViewer scrollViewer)
    {
        GraphicalUiElement barVisual = scrollViewer.Visual.GetGraphicalUiElementByName("VerticalScrollBarInstance")!;
        return (ScrollBar)((InteractiveGue)barVisual).FormsControlAsObject!;
    }

    /// <summary>
    /// Registers a behavior declaring the given Forms properties (with the optional declared
    /// default) and a component whose default state authors every property that has a value.
    /// </summary>
    private static void AuthorBehavior(
        InteractiveGue visual,
        List<(string Name, object? AuthoredValue)> properties,
        object? declaredDefault = null,
        string behaviorName = StandardFormsBehaviorNames.ScrollBarBehaviorName)
    {
        BehaviorSave behavior = new BehaviorSave { Name = behaviorName };
        ComponentSave component = new ComponentSave { Name = "Controls/Authored", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        foreach ((string name, object? authoredValue) in properties)
        {
            behavior.FormsProperties.Add(new VariableSave { Type = "double", Name = name, Value = declaredDefault });
            if (authoredValue != null)
            {
                defaultState.Variables.Add(new VariableSave
                {
                    Type = "double",
                    Name = name,
                    Value = authoredValue,
                    SetsValue = true
                });
            }
        }
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        visual.AddStates(new List<StateSave> { defaultState });
        visual.ElementSave = component;
    }
}
