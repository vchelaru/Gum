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
/// Live re-evaluation without the optional Gum.Expressions package, which is how Native AOT builds
/// run: only plain variable paths are read, but the triggers still have to work (ADR 0022, #5922).
/// </summary>
public class ReactiveVariableReferenceWithoutExpressionsTests : BaseTestClass
{
    public ReactiveVariableReferenceWithoutExpressionsTests()
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

    [Fact]
    public void ApplyAtTimeTo_AnimatedVariableReadByAPlainReference_UpdatesTheReferencingVariable()
    {
        ComponentSave element = CreateElement("Y = Progress");
        AnimationRuntime animation = CreateProgressAnimation(element);
        GraphicalUiElement gue = element.ToGraphicalUiElement();

        animation.ApplyAtTimeTo(0.5, gue);

        gue.Y.ShouldBe(50f);
    }

    [Fact]
    public void TypedProperty_ReadByAPlainReference_UpdatesTheReferencingVariable()
    {
        ComponentSave element = CreateElement("Y = X");
        GraphicalUiElement gue = element.ToGraphicalUiElement();

        gue.X = 5;

        gue.Y.ShouldBe(5f);
    }

    [Fact]
    public void RefreshReferences_ReadsTheCurrentValueOfTheNamedVariable()
    {
        ComponentSave element = CreateElement("Y = Width");
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.Width = 7;

        gue.RefreshReferences("Width");

        gue.Y.ShouldBe(7f);
    }

    private static ComponentSave CreateElement(params string[] rows)
    {
        ComponentSave element = new ComponentSave { Name = "Wave", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "Progress", Type = "float", Value = 0f, SetsValue = true });
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

    // Progress moves from 0 to 100 over one second.
    private static AnimationRuntime CreateProgressAnimation(ComponentSave element)
    {
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        foreach ((string name, float value) in new[] { ("Start", 0f), ("End", 100f) })
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
