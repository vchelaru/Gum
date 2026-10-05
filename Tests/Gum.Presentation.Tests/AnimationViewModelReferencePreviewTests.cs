using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins that scrubbing or playing an animation in the tool re-evaluates same-element variable
/// references whose source variable the animation touched (issue #5662).
/// </summary>
public class AnimationViewModelReferencePreviewTests
{
    public AnimationViewModelReferencePreviewTests()
    {
        GumExpressionService.Initialize();
    }

    [Fact]
    public void SetStateAtTime_ReevaluatesReference_WhoseSourceVariableIsAnimated()
    {
        ComponentSave element = CreateElement(extraReference: null);
        (AnimationViewModel animation, Mock<ISelectedState> selectedState) = CreateAnimation(element);

        animation.SetStateAtTime(0.5, element, defaultIfNull: true);

        StateSave shown = selectedState.Object.CustomCurrentStateSave!;
        shown.GetValue("Width").ShouldBe(150f);
        shown.GetValue("Height").ShouldBe(300f);
    }

    [Fact]
    public void SetStateAtTime_DoesNotReevaluateReference_WhoseSourceIsNotAnimated()
    {
        // Alpha = Rotation + 1 would give 1, but Rotation is never animated, so the row is skipped
        // and the authored 999 survives. Skipping keeps the per-tick cost proportional to the
        // rows the animation actually affects.
        ComponentSave element = CreateElement(extraReference: "Alpha = Rotation + 1");
        element.DefaultState.SetValue("Alpha", 999f, "float");
        (AnimationViewModel animation, Mock<ISelectedState> selectedState) = CreateAnimation(element);

        animation.SetStateAtTime(0.5, element, defaultIfNull: true);

        selectedState.Object.CustomCurrentStateSave!.GetValue("Alpha").ShouldBe(999f);
    }

    [Fact]
    public void SetStateAtTime_ReevaluatesChainedReference_ThroughAnIntermediateVariable()
    {
        // Rotation = Height / 3 depends on Height, which is itself a reference to the animated Width.
        ComponentSave element = CreateElement(extraReference: "Rotation = Height / 3");
        (AnimationViewModel animation, Mock<ISelectedState> selectedState) = CreateAnimation(element);

        animation.SetStateAtTime(0.5, element, defaultIfNull: true);

        selectedState.Object.CustomCurrentStateSave!.GetValue("Rotation").ShouldBe(100f);
    }

    private static ComponentSave CreateElement(string? extraReference)
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        element.States.Add(new StateSave { Name = "Default", ParentContainer = element });
        element.DefaultState.SetValue("Width", 100f, "float");
        element.DefaultState.SetValue("Height", 200f, "float");
        element.DefaultState.SetValue("Rotation", 0f, "float");

        List<string> references = new List<string> { "Height = Width * 2" };
        if (extraReference != null)
        {
            references.Add(extraReference);
        }
        element.DefaultState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "VariableReferences",
            Type = "string",
            Value = references
        });

        StateSave start = new StateSave { Name = "Start", ParentContainer = element };
        start.SetValue("Width", 100f, "float");
        StateSave end = new StateSave { Name = "End", ParentContainer = element };
        end.SetValue("Width", 200f, "float");
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        category.States.Add(start);
        category.States.Add(end);
        element.Categories.Add(category);
        return element;
    }

    private static (AnimationViewModel, Mock<ISelectedState>) CreateAnimation(ElementSave element)
    {
        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.SetupProperty(state => state.CustomCurrentStateSave);
        selectedState.SetupProperty(state => state.SelectedStateSave);
        selectedState.SetupGet(state => state.SelectedElement).Returns(element);
        IWireframeObjectManager wireframe = Mock.Of<IWireframeObjectManager>();

        AnimationViewModel animation = new AnimationViewModel(selectedState.Object, wireframe) { Name = "Anim" };
        animation.Keyframes.Add(new AnimatedKeyframeViewModel { StateName = "Cat/Start", Time = 0, InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear });
        animation.Keyframes.Add(new AnimatedKeyframeViewModel { StateName = "Cat/End", Time = 1 });
        animation.RefreshCumulativeStates(element);
        return (animation, selectedState);
    }
}
