using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Same-component variable references re-evaluate when a variable they read changes (ADR 0022, #5661).
/// </summary>
public class ReactiveVariableReferenceTests : BaseTestClass
{
    public ReactiveVariableReferenceTests()
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

    [Fact]
    public void ApplyAtTimeTo_AnimatedVariableReadByAReference_UpdatesTheReferencingVariable()
    {
        ComponentSave element = CreateElement("Y = Progress * 10");
        AnimationRuntime animation = CreateProgressAnimation(element, startProgress: 0f, endProgress: 5f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();

        animation.ApplyAtTimeTo(0.5, gue);

        gue.Y.ShouldBe(25f);
    }

    [Fact]
    public void ApplyAtTimeTo_ReferenceReadsAFunctionOfTheAnimatedVariable_FollowsItEachFrame()
    {
        ComponentSave element = CreateElement("Y = Sin(Progress) * 100");
        // Sin takes degrees.
        AnimationRuntime animation = CreateProgressAnimation(element, startProgress: 0f, endProgress: 180f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();

        animation.ApplyAtTimeTo(0.25, gue);
        float atQuarter = gue.Y;
        animation.ApplyAtTimeTo(0.5, gue);
        float atHalf = gue.Y;

        atQuarter.ShouldBe(70.71f, 0.01f);
        atHalf.ShouldBe(100f, 0.01f);
    }

    [Fact]
    public void ApplyAtTimeTo_ReferencesChainedOutOfOrder_AllFollowTheAnimatedVariable()
    {
        ComponentSave element = CreateElement("Y = Offset + 1", "Offset = Progress * 10");
        AnimationRuntime animation = CreateProgressAnimation(element, startProgress: 0f, endProgress: 5f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();

        animation.ApplyAtTimeTo(1.0, gue);

        gue.Y.ShouldBe(51f);
    }

    [Fact]
    public void ApplyAtTimeTo_ReferenceNotReadingTheAnimatedVariable_IsNotEvaluated()
    {
        ComponentSave element = CreateElement("Y = Progress * 10", "X = Other + 1");
        AnimationRuntime animation = CreateProgressAnimation(element, startProgress: 0f, endProgress: 5f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.X = 500;

        animation.ApplyAtTimeTo(1.0, gue);

        // X was set to 500 in code after load and no animated variable feeds its row.
        gue.X.ShouldBe(500f);
        gue.Y.ShouldBe(50f);
    }

    [Fact]
    public void RefreshReferences_AfterSettingATypedProperty_UpdatesRowsReadingIt()
    {
        ComponentSave element = CreateElement("Y = Offset * 2");
        GraphicalUiElement gue = element.ToGraphicalUiElement();

        // Offset is a custom variable with no typed property, so it is reported by value.
        gue.NotifyVariableChanged("Offset", 7f);

        gue.Y.ShouldBe(14f);
    }

    [Fact]
    public void RefreshReferences_ReadsTheCurrentValueOfTheNamedVariable()
    {
        ComponentSave element = CreateElement("Y = X * 2");
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.X = 5;

        gue.RefreshReferences("X");

        gue.Y.ShouldBe(10f);
    }

    [Fact]
    public void RefreshReferences_NoRowReadsTheVariable_ChangesNothing()
    {
        ComponentSave element = CreateElement("Y = Offset * 2");
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.Y = 99;

        gue.RefreshReferences("Other");

        gue.Y.ShouldBe(99f);
    }

    private static ComponentSave CreateElement(params string[] rows)
    {
        ComponentSave element = new ComponentSave { Name = "Wave", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "Progress", Type = "float", Value = 0f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Offset", Type = "float", Value = 0f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Other", Type = "float", Value = 0f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 0f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Y", Type = "float", Value = 0f, SetsValue = true });

        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        list.Value.AddRange(rows);
        state.VariableLists.Add(list);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(element);
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container"));
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;
        return element;
    }

    // Progress moves from start to end over one second.
    private static AnimationRuntime CreateProgressAnimation(ComponentSave element, float startProgress, float endProgress)
    {
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        foreach ((string name, float value) in new[] { ("Start", startProgress), ("End", endProgress) })
        {
            StateSave state = new StateSave { Name = name, ParentContainer = element };
            state.Variables.Add(new VariableSave { Name = "Progress", Type = "float", Value = value, SetsValue = true });
            category.States.Add(state);
        }
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
}
