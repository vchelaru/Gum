using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Where a reference reads its inputs from: the live visual, the stored state, the base component, a screen
/// override or another element, on a first apply, a live re-evaluation, ApplyAllVariableReferences and the
/// tool's ApplyVariableReferences(ElementSave). Pins the current answers; see the 8024 model doc.
/// </summary>
public class VariableReferenceLiveReadTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    internal static GumProjectSave NewProject(bool useExpressions)
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
        GumProjectSave project = new GumProjectSave();
        foreach (string name in new[] { "Container", "Text" })
        {
            StandardElementSave element = new StandardElementSave { Name = name };
            element.States.Add(StandardElementsManager.Self.GetDefaultStateFor(name)!);
            element.DefaultState!.ParentContainer = element;
            project.StandardElements.Add(element);
        }
        StandardRuntimeRegistrations.Register();
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    internal static ComponentSave NewComponent(GumProjectSave project, string name, string baseType, bool withInstances, string? targetRow)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);
        if (withInstances)
        {
            component.Instances.Add(new InstanceSave { Name = "Source", BaseType = "Container", ParentContainer = component });
            component.Instances.Add(new InstanceSave { Name = "Target", BaseType = "Container", ParentContainer = component });
        }
        if (targetRow != null)
        {
            VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "Target.VariableReferences" };
            list.Value.Add(targetRow);
            state.VariableLists.Add(list);
        }
        project.Components.Add(component);
        return component;
    }

    internal static void Store(StateSave state, string name, object value)
    {
        state.Variables.Add(new VariableSave { Name = name, Type = "float", Value = value, SetsValue = true });
    }

    internal static object? StoredValue(ComponentSave component, string name) => component.DefaultState!.GetValueRecursive(name);

    // Panel: Source.Width stored 50, Target.Width stored 10, row Target.Width = Source.Width.
    private static (GumProjectSave Project, ComponentSave Component, GraphicalUiElement Panel, GraphicalUiElement Source, GraphicalUiElement Target) BuildPair(bool useExpressions)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "Width = Source.Width");
        Store(component.DefaultState!, "Source.Width", 50f);
        Store(component.DefaultState!, "Target.Width", 10f);
        GraphicalUiElement panel = component.ToGraphicalUiElement();
        return (project, component, panel, panel.GetGraphicalUiElementByName("Source")!, panel.GetGraphicalUiElementByName("Target")!);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstApply_SourceStoredInOwnerState_TargetReadsTheStoredValue(bool useExpressions)
    {
        var pair = BuildPair(useExpressions);

        pair.Target.Width.ShouldBe(50f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstApply_SourceNeverSet_TargetReadsTheStandardDefault(bool useExpressions)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "Width = Source.Width");
        Store(component.DefaultState!, "Target.Width", 10f);

        GraphicalUiElement panel = component.ToGraphicalUiElement();

        panel.GetGraphicalUiElementByName("Source")!.Width.ShouldBe(150f);
        panel.GetGraphicalUiElementByName("Target")!.Width.ShouldBe(150f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Live_SourceSetByCodeOnTheVisual_TargetFollowsTheVisual(bool useExpressions)
    {
        var pair = BuildPair(useExpressions);

        pair.Source.Width = 77;

        pair.Target.Width.ShouldBe(77f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Live_CodeSetSourceThenApplyAll_TargetKeepsTheLiveValueAndTheStateKeepsTheStoredOne(bool useExpressions)
    {
        var pair = BuildPair(useExpressions);
        pair.Source.Width = 77;

        pair.Project.ApplyAllVariableReferences();

        // pins current behavior; see model doc, inconsistency 1
        pair.Target.Width.ShouldBe(77f);
        StoredValue(pair.Component, "Target.Width").ShouldBe(50f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Live_CodeSetSourceThenRefreshStyles_BothGoBackToTheStoredValue(bool useExpressions)
    {
        var pair = BuildPair(useExpressions);
        pair.Source.Width = 77;

        pair.Panel.RefreshStyles();

        // pins current behavior; see model doc, inconsistency 1
        pair.Source.Width.ShouldBe(50f);
        pair.Target.Width.ShouldBe(50f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToolApply_CodeSetSourceVisual_ReadsTheStoredStateNotTheVisual(bool useExpressions)
    {
        var pair = BuildPair(useExpressions);
        pair.Source.Width = 77;
        pair.Component.DefaultState!.SetValue("Target.Width", 1f, "float");

        pair.Component.ApplyVariableReferences(pair.Component.DefaultState!);

        // pins current behavior; see model doc, inconsistency 1
        StoredValue(pair.Component, "Target.Width").ShouldBe(50f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToolApply_LiveRootGiven_StillReadsTheStoredStateForNonAbsoluteNames(bool useExpressions)
    {
        var pair = BuildPair(useExpressions);
        pair.Source.Width = 77;
        pair.Component.DefaultState!.SetValue("Target.Width", 1f, "float");

        pair.Component.ApplyVariableReferences(pair.Component.DefaultState!, pair.Panel);

        // pins current behavior; see model doc, inconsistency 1
        StoredValue(pair.Component, "Target.Width").ShouldBe(50f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Live_CustomVariableSetByCode_TargetFollows(bool useExpressions)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "X = Offset");
        Store(component.DefaultState!, "Offset", 5f);
        GraphicalUiElement panel = component.ToGraphicalUiElement();
        GraphicalUiElement target = panel.GetGraphicalUiElementByName("Target")!;
        target.X.ShouldBe(5f);

        panel.SetProperty("Offset", 11f);

        target.X.ShouldBe(11f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstApply_CustomVariableStoredInBaseComponent_TargetReadsTheBaseValue(bool useExpressions)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave baseComponent = NewComponent(project, "Base", "Container", false, null);
        Store(baseComponent.DefaultState!, "Offset", 60f);
        ComponentSave derived = NewComponent(project, "Panel", "Base", true, "X = Offset");

        GraphicalUiElement panel = derived.ToGraphicalUiElement();

        panel.GetGraphicalUiElementByName("Target")!.X.ShouldBe(60f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstApply_CustomVariableStoredInBaseAndDerived_TargetReadsTheDerivedValue(bool useExpressions)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave baseComponent = NewComponent(project, "Base", "Container", false, null);
        Store(baseComponent.DefaultState!, "Offset", 60f);
        ComponentSave derived = NewComponent(project, "Panel", "Base", true, "X = Offset");
        Store(derived.DefaultState!, "Offset", 40f);

        GraphicalUiElement panel = derived.ToGraphicalUiElement();

        panel.GetGraphicalUiElementByName("Target")!.X.ShouldBe(40f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstApply_ScreenOverridesTheOwnersWidthAReferenceReads_TargetReadsTheOverride(bool useExpressions)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "Width = Width");
        Store(component.DefaultState!, "Width", 100f);
        ScreenSave screen = new ScreenSave { Name = "Scr" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Panel1", BaseType = "Panel", ParentContainer = screen });
        Store(screen.DefaultState!, "Panel1.Width", 300f);
        project.Screens.Add(screen);

        GraphicalUiElement root = screen.ToGraphicalUiElement();
        GraphicalUiElement panel = root.GetGraphicalUiElementByName("Panel1")!;

        panel.Width.ShouldBe(300f);
        panel.GetGraphicalUiElementByName("Target")!.Width.ShouldBe(300f);

        panel.Width = 400;

        panel.GetGraphicalUiElementByName("Target")!.Width.ShouldBe(400f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrossElement_OtherComponentsInstanceChangedByCode_TargetStillReadsTheStoredValue(bool useExpressions)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave other = NewComponent(project, "Other", "Container", true, null);
        Store(other.DefaultState!, "Source.Width", 33f);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "Width = Components/Other.Source.Width");
        GraphicalUiElement otherGue = other.ToGraphicalUiElement();
        otherGue.GetGraphicalUiElementByName("Source")!.Width = 99;

        GraphicalUiElement panel = component.ToGraphicalUiElement();

        // pins current behavior; see model doc, inconsistency 3
        panel.GetGraphicalUiElementByName("Target")!.Width.ShouldBe(33f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Live_CategoryStateSetsSource_TargetFollows(bool useExpressions)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "Width = Source.Width");
        Store(component.DefaultState!, "Source.Width", 50f);
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        StateSave wide = new StateSave { Name = "Wide", ParentContainer = component };
        Store(wide, "Source.Width", 200f);
        category.States.Add(wide);
        component.Categories.Add(category);
        GraphicalUiElement panel = component.ToGraphicalUiElement();

        panel.ApplyState("Cat", "Wide");

        panel.GetGraphicalUiElementByName("Source")!.Width.ShouldBe(200f);
        panel.GetGraphicalUiElementByName("Target")!.Width.ShouldBe(200f);

        project.ApplyAllVariableReferences();

        panel.GetGraphicalUiElementByName("Target")!.Width.ShouldBe(200f);
    }

    [Fact]
    public void FirstApply_AbsoluteWidthOfSource_ReadsTheLaidOutValueWithExpressions()
    {
        GumProjectSave project = NewProject(true);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "Width = Source.AbsoluteWidth");
        Store(component.DefaultState!, "Source.Width", 50f);

        GraphicalUiElement panel = component.ToGraphicalUiElement();

        // rows reading a laid-out value are evaluated again once the build has laid out (8026)
        panel.GetGraphicalUiElementByName("Target")!.Width.ShouldBe(50f);
    }

    [Fact]
    public void Live_AbsoluteWidthOfSourceAfterCodeSet_ReadsTheLaidOutValueWithExpressions()
    {
        GumProjectSave project = NewProject(true);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "Width = Source.AbsoluteWidth");
        Store(component.DefaultState!, "Source.Width", 50f);
        GraphicalUiElement panel = component.ToGraphicalUiElement();

        panel.GetGraphicalUiElementByName("Source")!.Width = 80;

        // pins current behavior; see model doc, inconsistency 2
        panel.GetGraphicalUiElementByName("Target")!.Width.ShouldBe(80f);
    }


    [Fact]
    public void FirstApply_AbsoluteWidthInNestedComponent_ReadsTheLaidOutValueWithExpressions()
    {
        GumProjectSave project = NewProject(true);
        ComponentSave inner = NewComponent(project, "Inner", "Container", true, "Width = Source.AbsoluteWidth");
        Store(inner.DefaultState!, "Source.Width", 50f);
        ComponentSave outer = NewComponent(project, "Outer", "Container", false, null);
        outer.Instances.Add(new InstanceSave { Name = "Child", BaseType = "Inner", ParentContainer = outer });

        GraphicalUiElement panel = outer.ToGraphicalUiElement();

        GraphicalUiElement child = panel.GetGraphicalUiElementByName("Child")!;
        child.GetGraphicalUiElementByName("Target")!.Width.ShouldBe(50f);
    }

    [Fact]
    public void FirstApply_AbsoluteWidthWithoutExpressions_RowIsNotApplied()
    {
        GumProjectSave project = NewProject(false);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "Width = Source.AbsoluteWidth");
        Store(component.DefaultState!, "Source.Width", 50f);
        Store(component.DefaultState!, "Target.Width", 10f);

        GraphicalUiElement panel = component.ToGraphicalUiElement();

        // pins current behavior; see model doc, inconsistency 2
        panel.GetGraphicalUiElementByName("Target")!.Width.ShouldBe(10f);
    }
}
