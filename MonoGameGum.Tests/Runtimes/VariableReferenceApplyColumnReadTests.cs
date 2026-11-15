using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// What ApplyAllVariableReferences, the tool ApplyVariableReferences(ElementSave), Index, the @ prefix and an
/// animation keyframe read for sources that are defaulted, stored in a base component or stored in a category
/// state. Pins the current answers; see the 8024 model doc.
/// </summary>
public class VariableReferenceApplyColumnReadTests : BaseTestClass
{
    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        base.Dispose();
    }

    private static GumProjectSave NewProject(bool useExpressions) => VariableReferenceLiveReadTests.NewProject(useExpressions);

    private static ComponentSave NewComponent(GumProjectSave project, string name, string baseType, bool withInstances, string? row) =>
        VariableReferenceLiveReadTests.NewComponent(project, name, baseType, withInstances, row);

    private static void Store(StateSave state, string name, object value) => VariableReferenceLiveReadTests.Store(state, name, value);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyAll_SourceNeverSet_StateGetsTheStandardDefault(bool useExpressions)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "Width = Source.Width");
        Store(component.DefaultState!, "Target.Width", 10f);

        project.ApplyAllVariableReferences();

        component.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(150f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToolApply_SourceNeverSet_StateGetsTheStandardDefault(bool useExpressions)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "Width = Source.Width");
        Store(component.DefaultState!, "Target.Width", 10f);

        component.ApplyVariableReferences(component.DefaultState!);

        component.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(150f);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ApplyAllAndToolApply_CustomVariableInBaseComponent_StateGetsTheBaseValue(bool useExpressions, bool tool)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave baseComponent = NewComponent(project, "Base", "Container", false, null);
        Store(baseComponent.DefaultState!, "Offset", 60f);
        ComponentSave derived = NewComponent(project, "Panel", "Base", true, "X = Offset");

        if (tool)
        {
            derived.ApplyVariableReferences(derived.DefaultState!);
        }
        else
        {
            project.ApplyAllVariableReferences();
        }

        derived.DefaultState!.GetValueRecursive("Target.X").ShouldBe(60f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyAll_RowInACategoryState_ReadsTheCategoryStateThenTheDefaultState(bool useExpressions)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, null);
        Store(component.DefaultState!, "Source.Width", 50f);
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        StateSave withSource = new StateSave { Name = "WithSource", ParentContainer = component };
        Store(withSource, "Source.Width", 200f);
        StateSave withoutSource = new StateSave { Name = "WithoutSource", ParentContainer = component };
        foreach (StateSave state in new[] { withSource, withoutSource })
        {
            VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "Target.VariableReferences" };
            list.Value.Add("Width = Source.Width");
            state.VariableLists.Add(list);
            category.States.Add(state);
        }
        component.Categories.Add(category);

        project.ApplyAllVariableReferences();

        // pins current behavior: a categorized state reads its own value, else the default state value
        withSource.GetValueRecursive("Target.Width").ShouldBe(200f);
        withoutSource.GetValueRecursive("Target.Width").ShouldBe(50f);
    }

    [Fact]
    public void FirstApply_IndexOfTheTargetAmongSiblings_WithExpressionsIsItsPosition()
    {
        GumProjectSave project = NewProject(true);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "X = @Index * 10");

        GraphicalUiElement panel = component.ToGraphicalUiElement();

        panel.GetGraphicalUiElementByName("Target")!.X.ShouldBe(10f);
    }

    [Fact]
    public void ApplyAllAndToolApply_IndexOfTheTarget_WithExpressionsIsItsPosition()
    {
        GumProjectSave project = NewProject(true);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "X = @Index * 10");

        project.ApplyAllVariableReferences();
        object? afterApplyAll = component.DefaultState!.GetValueRecursive("Target.X");
        component.DefaultState!.SetValue("Target.X", 1f, "float");
        component.ApplyVariableReferences(component.DefaultState!);

        afterApplyAll.ShouldBe(10f);
        component.DefaultState!.GetValueRecursive("Target.X").ShouldBe(10f);
    }

    [Fact]
    public void FirstApply_IndexWithoutExpressions_RowIsNotApplied()
    {
        GumProjectSave project = NewProject(false);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "X = @Index");
        Store(component.DefaultState!, "Target.X", 7f);

        GraphicalUiElement panel = component.ToGraphicalUiElement();

        // pins current behavior; Index needs Gum.Expressions
        panel.GetGraphicalUiElementByName("Target")!.X.ShouldBe(7f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OwnerPrefix_AtHeightOnTheTarget_ReadsStoredAtLoadAndTheLiveVisualAfterAChange(bool useExpressions)
    {
        GumProjectSave project = NewProject(useExpressions);
        ComponentSave component = NewComponent(project, "Panel", "Container", true, "Width = @Height");
        Store(component.DefaultState!, "Target.Height", 30f);
        GraphicalUiElement panel = component.ToGraphicalUiElement();
        GraphicalUiElement target = panel.GetGraphicalUiElementByName("Target")!;
        target.Width.ShouldBe(30f);

        target.Height = 44;

        target.Width.ShouldBe(44f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Keyframe_RowReadsTheLiveCustomVariable_WhileApplyAllStoresTheAuthoredOne(bool useExpressions)
    {
        NewProject(useExpressions);
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = OffsetX", startStoredX: 0f, endRow: "X = OffsetX", endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("OffsetX", 50f);

        animation.ApplyAtTimeTo(0.5, gue);
        float live = gue.X;
        ObjectFinder.Self.GumProjectSave!.ApplyAllVariableReferences();
        object? stored = element.Categories[0].States[0].GetValueRecursive("X");

        live.ShouldBe(50f);
        // pins current behavior; see model doc, inconsistency 7: the stored keyframe holds the authored OffsetX
        stored.ShouldBe(5f);
    }
}
