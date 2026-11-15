using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Moq;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins that the tool's animation preview resolves a keyframe state's variable-reference rows
/// (<c>X = OffsetX</c>) the way the runtime does (issue #7645): the keyframe holds what the
/// component's variable reads now, then the preview interpolates.
/// </summary>
public class AnimationViewModelKeyframeReferencePreviewTests : BaseTestClass
{
    public AnimationViewModelKeyframeReferencePreviewTests()
    {
        StandardElementsManager.Self.Initialize();
        GumExpressionService.Initialize();
    }

    [Theory]
    [InlineData(0.0, 10f)]
    [InlineData(0.5, 60f)]
    [InlineData(1.0, 110f)]
    public void SetStateAtTime_KeyframesReferenceCustomVariables_InterpolatesTheirValues(double time, float expectedX)
    {
        ComponentSave element = CreateElement(offsetX: 10f, offsetEnd: 110f);
        (AnimationViewModel animation, Mock<ISelectedState> selectedState) = CreateAnimation(element, offsetX: 10f, offsetEnd: 110f, "X = OffsetX", "X = OffsetEnd");

        animation.SetStateAtTime(time, element, defaultIfNull: true);

        selectedState.Object.CustomCurrentStateSave!.GetValue("X").ShouldBe(expectedX);
    }

    [Fact]
    public void SetStateAtTime_OffsetEditedBetweenTicks_TakesEffectOnTheNextTick()
    {
        ComponentSave element = CreateElement(offsetX: 10f, offsetEnd: 110f);
        (AnimationViewModel animation, Mock<ISelectedState> selectedState) = CreateAnimation(element, offsetX: 10f, offsetEnd: 110f, "X = OffsetX", "X = OffsetEnd");
        animation.SetStateAtTime(0.5, element, defaultIfNull: true);
        selectedState.Object.CustomCurrentStateSave!.GetValue("X").ShouldBe(60f);

        _root.SetProperty("OffsetX", 30f);
        animation.SetStateAtTime(0.5, element, defaultIfNull: true);

        selectedState.Object.CustomCurrentStateSave!.GetValue("X").ShouldBe(70f);
    }

    [Fact]
    public void SetStateAtTime_ExpressionRow_AddsTheOffsetToTheKeyframeValue()
    {
        ComponentSave element = CreateElement(offsetX: 10f, offsetEnd: 110f);
        (AnimationViewModel animation, Mock<ISelectedState> selectedState) = CreateAnimation(element, offsetX: 10f, offsetEnd: 110f, "X = 100 + OffsetX", "X = 200 + OffsetX");

        animation.SetStateAtTime(0.5, element, defaultIfNull: true);

        selectedState.Object.CustomCurrentStateSave!.GetValue("X").ShouldBe(160f);
    }

    [Fact]
    public void SetStateAtTime_UnresolvableRow_KeepsTheStoredValue()
    {
        ComponentSave element = CreateElement(offsetX: 10f, offsetEnd: 110f);
        (AnimationViewModel animation, Mock<ISelectedState> selectedState) = CreateAnimation(element, offsetX: 10f, offsetEnd: 110f, "X = Missing", "X = Missing");

        animation.SetStateAtTime(0.5, element, defaultIfNull: true);

        selectedState.Object.CustomCurrentStateSave!.GetValue("X").ShouldBe(100f);
    }

    private static ComponentSave CreateElement(float offsetX, float offsetEnd)
    {
        ComponentSave element = new ComponentSave { Name = "Wave", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "OffsetX", Type = "float", Value = offsetX, SetsValue = true, IsCustomVariable = true });
        state.Variables.Add(new VariableSave { Name = "OffsetEnd", Type = "float", Value = offsetEnd, SetsValue = true, IsCustomVariable = true });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(element);
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!);
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;
        return element;
    }

    private GraphicalUiElement _root = null!;

    private (AnimationViewModel, Mock<ISelectedState>) CreateAnimation(ComponentSave element, float offsetX, float offsetEnd, string startRow, string endRow)
    {
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        category.States.Add(CreateState(element, "Start", startRow));
        category.States.Add(CreateState(element, "End", endRow));
        element.Categories.Add(category);

        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.SetupProperty(state => state.CustomCurrentStateSave);
        selectedState.SetupProperty(state => state.SelectedStateSave);
        selectedState.SetupGet(state => state.SelectedElement).Returns(element);

        _root = new GraphicalUiElement(new InvisibleRenderable()) { ElementSave = element };
        _root.SetProperty("OffsetX", offsetX);
        _root.SetProperty("OffsetEnd", offsetEnd);
        Mock<IWireframeObjectManager> wireframe = new Mock<IWireframeObjectManager>();
        wireframe.SetupGet(w => w.RootGue).Returns(_root);

        AnimationViewModel animation = new AnimationViewModel(selectedState.Object, wireframe.Object) { Name = "Anim" };
        animation.Keyframes.Add(new AnimatedKeyframeViewModel { StateName = "Cat/Start", Time = 0, InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear });
        animation.Keyframes.Add(new AnimatedKeyframeViewModel { StateName = "Cat/End", Time = 1 });
        animation.RefreshCumulativeStates(element);
        return (animation, selectedState);
    }

    // Stores X = 0 / 200 next to the row, as a state that never resolved its row would hold.
    private static StateSave CreateState(ComponentSave element, string name, string row)
    {
        StateSave state = new StateSave { Name = name, ParentContainer = element };
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = name == "Start" ? 0f : 200f, SetsValue = true });
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        list.Value.Add(row);
        state.VariableLists.Add(list);
        return state;
    }
}
