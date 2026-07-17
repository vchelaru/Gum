using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="AnimationViewModel.HasBrokenKeyframe"/>, the flag the animation list uses to
/// show a broken-keyframe indicator without replacing the functional loop toggle (issue #3401).
/// Mirrors <see cref="AnimatedKeyframeViewModel.IsMissingReference"/> aggregated across keyframes.
/// Relocated out of GumToolUnitTests into headless Gum.Presentation.Tests once AnimationViewModel's
/// dead WPF BitmapFrame plumbing was removed (ADR-0005, issue #3754).
/// </summary>
public class AnimationViewModelBrokenKeyframeTests
{
    private readonly ISelectedState _selectedState = Mock.Of<ISelectedState>();
    private readonly IWireframeObjectManager _wireframeObjectManager = Mock.Of<IWireframeObjectManager>();

    [Fact]
    public void ChangingKeyframeHasValidState_RaisesPropertyChanged_ForHasBrokenKeyframe()
    {
        AnimatedKeyframeViewModel keyframe = Keyframe(stateName: "Cat/Idle", hasValidState: true);
        AnimationViewModel animation = CreateAnimation(keyframe);
        List<string> changed = new List<string>();
        animation.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? "");

        keyframe.HasValidState = false;

        changed.ShouldContain(nameof(AnimationViewModel.HasBrokenKeyframe));
    }

    [Fact]
    public void HasBrokenKeyframe_IsFalse_WhenAllKeyframesAreValid()
    {
        AnimationViewModel animation = CreateAnimation(
            Keyframe(stateName: "Cat/Idle", hasValidState: true),
            Keyframe(eventName: "OnSomething", hasValidState: false));

        animation.HasBrokenKeyframe.ShouldBeFalse();
    }

    [Fact]
    public void HasBrokenKeyframe_IsTrue_WhenAnyKeyframeHasMissingReference()
    {
        AnimationViewModel animation = CreateAnimation(
            Keyframe(stateName: "Cat/Idle", hasValidState: true),
            Keyframe(stateName: "Cat/Missing", hasValidState: false));

        animation.HasBrokenKeyframe.ShouldBeTrue();
    }

    [Fact]
    public void RefreshErrors_UpdatesHasBrokenKeyframe_WhenMissingStateIsDetected()
    {
        Gum.DataTypes.ComponentSave element = ElementWithCategorizedState("Cat", "Idle");
        AnimatedKeyframeViewModel keyframe = Keyframe(stateName: "Cat/Missing", hasValidState: true);
        AnimationViewModel animation = CreateAnimation(keyframe);

        animation.HasBrokenKeyframe.ShouldBeFalse();

        animation.RefreshErrors(element);

        animation.HasBrokenKeyframe.ShouldBeTrue();
    }

    private static Gum.DataTypes.ComponentSave ElementWithCategorizedState(string categoryName, string stateName)
    {
        Gum.DataTypes.Variables.StateSaveCategory category = new Gum.DataTypes.Variables.StateSaveCategory { Name = categoryName };
        category.States.Add(new Gum.DataTypes.Variables.StateSave { Name = stateName });
        Gum.DataTypes.ComponentSave element = new Gum.DataTypes.ComponentSave { Name = "Foo" };
        element.Categories.Add(category);
        return element;
    }

    private AnimatedKeyframeViewModel Keyframe(string? stateName = null, string? eventName = null, bool hasValidState = false)
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel();
        if (stateName != null)
        {
            keyframe.StateName = stateName;
        }
        if (eventName != null)
        {
            keyframe.EventName = eventName;
        }
        keyframe.HasValidState = hasValidState;
        return keyframe;
    }

    private AnimationViewModel CreateAnimation(params AnimatedKeyframeViewModel[] keyframes)
    {
        AnimationViewModel animation = new AnimationViewModel(_selectedState, _wireframeObjectManager) { Name = "Anim" };
        foreach (AnimatedKeyframeViewModel keyframe in keyframes)
        {
            animation.Keyframes.Add(keyframe);
        }
        return animation;
    }
}
