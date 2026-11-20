using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Gum.ProjectServices.Editing;
using Gum.StateAnimation.SaveClasses;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class AnimationEditLogicTests : BaseTestClass
{
    private static AnimationEditLogic CreateLogic() => new AnimationEditLogic(new ElementNameValidator(topLevelNameSource: null));

    [Fact]
    public void GetWhyCannotAddAnimation_RefusesEmptySpacedDuplicateAndTopLevelNames()
    {
        ComponentSave popup = EditingTestProject.AddComponent(Project, "Popup");
        EditingTestProject.AddInstance(popup, "Spinner", "Container");
        EditingTestProject.AddCategory(popup, "Visibility", "Shown");
        ElementAnimationsSave animations = new ElementAnimationsSave();
        AnimationEditLogic logic = CreateLogic();
        logic.AddAnimation(animations, "FadeIn", loops: false);

        logic.GetWhyCannotAddAnimation(popup, animations, "").ShouldNotBeNull();
        logic.GetWhyCannotAddAnimation(popup, animations, "Fade Out")!.ShouldContain("spaces");
        logic.GetWhyCannotAddAnimation(popup, animations, "fadein")!.ShouldContain("already");
        logic.GetWhyCannotAddAnimation(popup, animations, "Spinner")!.ShouldContain("Instance");
        logic.GetWhyCannotAddAnimation(popup, animations, "Visibility")!.ShouldContain("Category");
        logic.GetWhyCannotAddAnimation(popup, animations, "FadeOut").ShouldBeNull();
    }

    [Fact]
    public void AddKeyframe_InsertsInTimeOrderAfterKeyframesAtTheSameTime()
    {
        AnimationSave animation = new AnimationSave { Name = "Pulse" };
        AnimationEditLogic logic = CreateLogic();

        logic.AddKeyframe(animation, "B", 1f, InterpolationType.Linear, Easing.Out);
        logic.AddKeyframe(animation, "A", 0f, InterpolationType.Linear, Easing.Out);
        logic.AddKeyframe(animation, "C", 1f, InterpolationType.Linear, Easing.Out);

        animation.States.Select(item => item.StateName).ShouldBe(new[] { "A", "B", "C" });
    }

    [Fact]
    public void AddKeyframe_StoresInterpolationAndEasing()
    {
        AnimationSave animation = new AnimationSave { Name = "Pulse" };

        AnimatedStateSave keyframe = CreateLogic().AddKeyframe(animation, "A", 2f, InterpolationType.Exponential, Easing.InOut);

        keyframe.InterpolationType.ShouldBe(InterpolationType.Exponential);
        keyframe.Easing.ShouldBe(Easing.InOut);
        keyframe.Time.ShouldBe(2f);
    }

    [Fact]
    public void GetWhyCannotAddKeyframe_RefusesUnknownStatesBadTimesAndDuplicates()
    {
        ComponentSave popup = EditingTestProject.AddComponent(Project, "Popup");
        EditingTestProject.AddCategory(popup, "Visibility", "Shown");
        popup.States.Add(new Gum.DataTypes.Variables.StateSave { Name = "Pressed", ParentContainer = popup });
        AnimationEditLogic logic = CreateLogic();
        AnimationSave animation = new AnimationSave { Name = "FadeIn" };
        logic.AddKeyframe(animation, "Pressed", 0f, InterpolationType.Linear, Easing.Out);

        logic.GetWhyCannotAddKeyframe(popup, animation, "Visibility/Shown", 1f).ShouldBeNull();
        logic.GetWhyCannotAddKeyframe(popup, animation, "Pressed", 1f).ShouldBeNull();
        logic.GetWhyCannotAddKeyframe(popup, animation, "Shown", 1f)!.ShouldContain("Category/State");
        logic.GetWhyCannotAddKeyframe(popup, animation, "Visibility/Gone", 1f).ShouldNotBeNull();
        logic.GetWhyCannotAddKeyframe(popup, animation, "Pressed", -1f)!.ShouldContain("time");
        logic.GetWhyCannotAddKeyframe(popup, animation, "Pressed", float.NaN)!.ShouldContain("time");
        logic.GetWhyCannotAddKeyframe(popup, animation, "Pressed", 0f)!.ShouldContain("already has");
    }

    [Fact]
    public void RemoveKeyframe_RemovesOnlyTheMatchingStateAndTime()
    {
        AnimationSave animation = new AnimationSave { Name = "Pulse" };
        AnimationEditLogic logic = CreateLogic();
        logic.AddKeyframe(animation, "A", 0f, InterpolationType.Linear, Easing.Out);
        logic.AddKeyframe(animation, "A", 1f, InterpolationType.Linear, Easing.Out);

        logic.RemoveKeyframe(animation, "A", 1f).ShouldBeTrue();
        logic.RemoveKeyframe(animation, "A", 1f).ShouldBeFalse();
        logic.RemoveKeyframe(animation, "B", 0f).ShouldBeFalse();

        animation.States.Select(item => item.Time).ShouldBe(new[] { 0f });
    }

    [Fact]
    public void RemoveAnimation_RemovesItByName()
    {
        ElementAnimationsSave animations = new ElementAnimationsSave();
        AnimationEditLogic logic = CreateLogic();
        logic.AddAnimation(animations, "FadeIn", loops: true);

        logic.RemoveAnimation(animations, "FadeIn").ShouldBeTrue();
        logic.RemoveAnimation(animations, "FadeIn").ShouldBeFalse();

        animations.Animations.ShouldBeEmpty();
    }
}
