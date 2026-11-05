using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using Gum.Expressions;
using MonoGameGum.GueDeriving;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A keyframe state's variable-reference rows are resolved against the live element every tick (#7645),
/// so a component can offset what an animation sets. Plain references need no package.
/// </summary>
public class AnimationKeyframeReferenceWithoutExpressionsTests : BaseTestClass
{
    public AnimationKeyframeReferenceWithoutExpressionsTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
    }

    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Theory]
    [InlineData(0.0, 10f)]
    [InlineData(0.5, 60f)]
    [InlineData(1.0, 110f)]
    public void ApplyAtTimeTo_BothKeyframesReferenceVariables_InterpolatesTheirValues(double time, float expectedX)
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = OffsetX", startStoredX: 0f, endRow: "X = OffsetEnd", endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("OffsetX", 10f);
        gue.SetProperty("OffsetEnd", 110f);

        animation.ApplyAtTimeTo(time, gue);

        gue.X.ShouldBe(expectedX);
    }

    [Fact]
    public void ApplyAtTimeTo_OffsetChangedBetweenTicks_TakesEffectOnTheNextTick()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = OffsetX", startStoredX: 0f, endRow: "X = OffsetEnd", endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("OffsetX", 10f);
        gue.SetProperty("OffsetEnd", 110f);
        animation.ApplyAtTimeTo(0.5, gue);
        gue.X.ShouldBe(60f);

        gue.SetProperty("OffsetX", 30f);
        animation.ApplyAtTimeTo(0.5, gue);

        gue.X.ShouldBe(70f);
    }

    [Fact]
    public void ApplyAtTimeTo_OffsetNeverSet_UsesTheDefaultOfTheVariable()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = OffsetX", startStoredX: 0f, endRow: "X = OffsetX", endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();

        animation.ApplyAtTimeTo(0.5, gue);

        // OffsetX defaults to 5 on the component.
        gue.X.ShouldBe(5f);
    }

    [Fact]
    public void ApplyAtTimeTo_OneKeyframeHasAReferenceAndTheOtherAStoredValue_InterpolatesBetweenThem()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: null, startStoredX: 0f, endRow: "X = OffsetEnd", endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("OffsetEnd", 100f);

        animation.ApplyAtTimeTo(0.5, gue);
        gue.X.ShouldBe(50f);
        animation.ApplyAtTimeTo(1, gue);
        gue.X.ShouldBe(100f);
    }

    [Fact]
    public void ApplyAtTimeTo_NoKeyframeHasAReference_UsesStoredValues()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: null, startStoredX: 0f, endRow: null, endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("OffsetX", 999f);

        animation.ApplyAtTimeTo(0.5, gue);

        gue.X.ShouldBe(100f);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(2.0)]
    public void ApplyAtTimeTo_TimeOutsideTheKeyframes_ResolvesTheNearestKeyframesReference(double time)
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = OffsetX", startStoredX: 0f, endRow: "X = OffsetEnd", endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("OffsetX", 10f);
        gue.SetProperty("OffsetEnd", 110f);

        animation.ApplyAtTimeTo(time, gue);

        gue.X.ShouldBe(time < 0 ? 10f : 110f);
    }

    [Fact]
    public void ApplyAtTimeTo_LoopingAnimation_ResolvesTheReferenceOnEachLoop()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = OffsetX", startStoredX: 0f, endRow: "X = OffsetEnd", endStoredX: 200f);
        animation.Loops = true;
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("OffsetX", 10f);
        gue.SetProperty("OffsetEnd", 110f);
        animation.ApplyAtTimeTo(0.5, gue);

        gue.SetProperty("OffsetX", 30f);
        animation.ApplyAtTimeTo(1.5, gue);

        gue.X.ShouldBe(70f);
    }

    [Fact]
    public void ApplyAtTimeTo_StateSharedByTwoElements_EachElementResolvesItsOwnOffset()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = OffsetX", startStoredX: 0f, endRow: "X = OffsetX", endStoredX: 200f);
        GraphicalUiElement first = element.ToGraphicalUiElement();
        GraphicalUiElement second = element.ToGraphicalUiElement();
        first.SetProperty("OffsetX", 10f);
        second.SetProperty("OffsetX", 90f);

        animation.ApplyAtTimeTo(0.5, first);
        animation.ApplyAtTimeTo(0.5, second);
        animation.ApplyAtTimeTo(0.5, first);

        first.X.ShouldBe(10f);
        second.X.ShouldBe(90f);
    }

    [Fact]
    public void ApplyAtTimeTo_Resolving_DoesNotChangeTheStoredKeyframeState()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = OffsetX", startStoredX: 0f, endRow: "X = OffsetEnd", endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("OffsetX", 10f);

        animation.ApplyAtTimeTo(0, gue);

        element.Categories[0].States[0].GetValue("X").ShouldBe(0f);
    }

    [Fact]
    public void ApplyAtTimeTo_UnresolvedReference_KeepsTheStoredValue()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = DoesNotExist", startStoredX: 40f, endRow: null, endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();

        Should.NotThrow(() => animation.ApplyAtTimeTo(0, gue));

        gue.X.ShouldBe(40f);
    }

    [Fact]
    public void ApplyAtTimeTo_ReferenceOfAnotherTypeFailingEveryTick_ReportsOnce()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = Flag", startStoredX: 40f, endRow: null, endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        List<string> reports = new List<string>();
        void Handler(string message) => reports.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            for (int i = 0; i < 20; i++)
            {
                animation.ApplyAtTimeTo(0, gue);
            }
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }

        gue.X.ShouldBe(40f);
        reports.Count.ShouldBe(1);
    }
}

/// <summary>Arithmetic in a keyframe row needs the optional Gum.Expressions package.</summary>
public class AnimationKeyframeReferenceTests : BaseTestClass
{
    public AnimationKeyframeReferenceTests()
    {
        StandardElementsManager.Self.Initialize();
        GumExpressionService.Initialize();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Theory]
    [InlineData(0.0, 110f)]
    [InlineData(0.5, 160f)]
    [InlineData(1.0, 210f)]
    public void ApplyAtTimeTo_RowAddsAnOffsetToAConstant_InterpolatesTheResolvedValues(double time, float expectedX)
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = 100 + OffsetX", startStoredX: 100f, endRow: "X = 200 + OffsetX", endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("OffsetX", 10f);

        animation.ApplyAtTimeTo(time, gue);

        gue.X.ShouldBe(expectedX);
    }

    [Fact]
    public void ApplyAtTimeTo_OffsetChangedMidAnimation_TakesEffectOnTheNextTick()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = 100 + OffsetX", startStoredX: 100f, endRow: "X = 200 + OffsetX", endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("OffsetX", 10f);
        animation.ApplyAtTimeTo(0.5, gue);

        gue.SetProperty("OffsetX", 50f);
        animation.ApplyAtTimeTo(0.5, gue);

        gue.X.ShouldBe(200f);
    }

    [Fact]
    public void ApplyAtTimeTo_RowReadsAnotherRowOfTheSameKeyframe_SeesItsResult()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = OffsetX * 2", startStoredX: 0f, endRow: null, endStoredX: 0f,
            extraStartRow: "Y = X + 1");
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("OffsetX", 10f);

        animation.ApplyAtTimeTo(0, gue);

        gue.X.ShouldBe(20f);
        gue.Y.ShouldBe(21f);
    }

    [Fact]
    public void ApplyAtTimeTo_ExpressionThatCannotBeEvaluated_KeepsTheStoredValue()
    {
        ComponentSave element = KeyframeReferenceHelper.CreateElement();
        AnimationRuntime animation = KeyframeReferenceHelper.CreateAnimation(element,
            startRow: "X = 100 + DoesNotExist", startStoredX: 40f, endRow: null, endStoredX: 200f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();

        Should.NotThrow(() => animation.ApplyAtTimeTo(0, gue));

        gue.X.ShouldBe(40f);
    }
}

internal static class KeyframeReferenceHelper
{
    // A component with custom floats OffsetX (default 5) and OffsetEnd and a custom bool Flag.
    public static ComponentSave CreateElement()
    {
        ComponentSave element = new ComponentSave { Name = "Wave", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "OffsetX", Type = "float", Value = 5f, SetsValue = true, IsCustomVariable = true });
        state.Variables.Add(new VariableSave { Name = "OffsetEnd", Type = "float", Value = 0f, SetsValue = true, IsCustomVariable = true });
        state.Variables.Add(new VariableSave { Name = "Flag", Type = "bool", Value = true, SetsValue = true, IsCustomVariable = true });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(element);
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container"));
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;
        return element;
    }

    // Keyframes at 0 and 1 second; each state stores X and, when given, a reference row.
    public static AnimationRuntime CreateAnimation(ComponentSave element, string? startRow, float startStoredX,
        string? endRow, float endStoredX, string? extraStartRow = null)
    {
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        category.States.Add(CreateState(element, "Start", startStoredX, startRow, extraStartRow));
        category.States.Add(CreateState(element, "End", endStoredX, endRow, null));
        element.Categories.Add(category);

        AnimationRuntime animation = new AnimationRuntime();
        animation.Keyframes.Add(new KeyframeRuntime
        {
            StateName = "Cat/Start",
            Time = 0,
            InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear
        });
        animation.Keyframes.Add(new KeyframeRuntime
        {
            StateName = "Cat/End",
            Time = 1,
            InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear
        });
        animation.RefreshCumulativeStates(element);
        return animation;
    }

    private static StateSave CreateState(ComponentSave element, string name, float storedX, string? row, string? extraRow)
    {
        StateSave state = new StateSave { Name = name, ParentContainer = element };
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = storedX, SetsValue = true });
        if (extraRow != null)
        {
            state.Variables.Add(new VariableSave { Name = "Y", Type = "float", Value = 0f, SetsValue = true });
        }
        if (row != null)
        {
            VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
            list.Value.Add(row);
            if (extraRow != null)
            {
                list.Value.Add(extraRow);
            }
            state.VariableLists.Add(list);
        }
        return state;
    }
}
