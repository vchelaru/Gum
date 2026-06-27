using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.StateAnimation.SaveClasses;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Pins the headless detection of animation keyframes that reference a missing state — the check
/// that lets the tree "!" / Errors surface animation errors on project open and on edits without
/// the State Animation plugin's per-selection view model (issue #3293).
/// </summary>
public class AnimationKeyframeErrorSourceTests
{
    private readonly Mock<IElementAnimationsProvider> _animationsProvider = new Mock<IElementAnimationsProvider>();

    [Fact]
    public void GetErrors_ReportsKeyframeReferencingMissingCategorizedState()
    {
        ComponentSave element = ElementWithCategorizedState("Cat", "Idle");
        GivenAnimations(element, AnimationWithStateKeyframe("Anim", "Cat/Missing"));

        List<ErrorResult> errors = CreateSut().GetErrors(element, new GumProjectSave()).ToList();

        errors.Count.ShouldBe(1);
        errors[0].ElementName.ShouldBe("Foo");
        errors[0].Message.ShouldContain("Cat/Missing");
    }

    [Fact]
    public void GetErrors_ReportsNoError_ForKeyframeReferencingExistingCategorizedState()
    {
        ComponentSave element = ElementWithCategorizedState("Cat", "Idle");
        GivenAnimations(element, AnimationWithStateKeyframe("Anim", "Cat/Idle"));

        CreateSut().GetErrors(element, new GumProjectSave()).ShouldBeEmpty();
    }

    [Fact]
    public void GetErrors_ReportsNoError_ForKeyframeReferencingExistingUncategorizedState()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        element.States.Add(new StateSave { Name = "Idle" });
        GivenAnimations(element, AnimationWithStateKeyframe("Anim", "Idle"));

        CreateSut().GetErrors(element, new GumProjectSave()).ShouldBeEmpty();
    }

    [Fact]
    public void GetErrors_ReportsNoError_WhenElementHasNoAnimations()
    {
        ComponentSave element = ElementWithCategorizedState("Cat", "Idle");
        _animationsProvider
            .Setup(provider => provider.GetAnimationsFor(element, It.IsAny<GumProjectSave>()))
            .Returns((ElementAnimationsSave?)null);

        CreateSut().GetErrors(element, new GumProjectSave()).ShouldBeEmpty();
    }

    private AnimationKeyframeErrorSource CreateSut()
    {
        return new AnimationKeyframeErrorSource(_animationsProvider.Object);
    }

    private void GivenAnimations(ElementSave element, ElementAnimationsSave animations)
    {
        _animationsProvider
            .Setup(provider => provider.GetAnimationsFor(element, It.IsAny<GumProjectSave>()))
            .Returns(animations);
    }

    private static ComponentSave ElementWithCategorizedState(string categoryName, string stateName)
    {
        StateSaveCategory category = new StateSaveCategory { Name = categoryName };
        category.States.Add(new StateSave { Name = stateName });
        ComponentSave element = new ComponentSave { Name = "Foo" };
        element.Categories.Add(category);
        return element;
    }

    private static ElementAnimationsSave AnimationWithStateKeyframe(string animationName, string keyframeStateName)
    {
        AnimationSave animation = new AnimationSave { Name = animationName };
        animation.States.Add(new AnimatedStateSave { StateName = keyframeStateName });
        ElementAnimationsSave animations = new ElementAnimationsSave();
        animations.Animations.Add(animation);
        return animations;
    }
}
