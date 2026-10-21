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
/// ScrollBar and ScrollViewer compute LargeChange from the viewport when a Visual is assigned.
/// A value authored on the Visual (or set in code) replaces that computation and survives a
/// Visual swap. See #6255.
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
