using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// The right-hand detail column per keyframe kind, and the interpolation curve the timeline draws
/// from the detail column's choices.
/// </summary>
public class DetailColumnTests
{
    private const string Category = "AnimationCategory";

    [AvaloniaFact]
    public void TheDetailColumn_ShowsStateAndInterpolationRows_OnlyForAStateKeyframe()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel state = editor.AddStateKeyframe($"{Category}/Pressed");

        editor.DetailTitle.ShouldBe("State");
        editor.DetailCombos.Count.ShouldBe(3);
        editor.DetailCombos.ShouldAllBe(combo => combo.IsEffectivelyVisible);
        editor.DetailTimeBox.IsEffectivelyVisible.ShouldBeTrue();

        editor.Dialogs.AnswerNextUserString("Footstep");
        editor.PickAddKeyframe("Named Event");
        AnimatedKeyframeViewModel footstep = walk.SelectedKeyframe.ShouldNotBeNull();

        editor.DetailTitle.ShouldBe("Footstep");
        editor.DetailCombos.ShouldAllBe(combo => !combo.IsEffectivelyVisible);
        editor.DetailTimeBox.IsEffectivelyVisible.ShouldBeTrue();
        editor.Detail.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "Interpolation Type").IsEffectivelyVisible.ShouldBeFalse();

        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");

        editor.DetailTitle.ShouldBe("Blink");
        editor.DetailCombos.ShouldAllBe(combo => !combo.IsEffectivelyVisible);

        editor.Click(editor.RowFor(editor.KeyframeList, state));
        editor.DetailTitle.ShouldBe("State");
        editor.DetailCombos.ShouldAllBe(combo => combo.IsEffectivelyVisible);

        walk.SelectedKeyframe = null;
        editor.Layout();
        editor.DetailTitle.ShouldBe("No Keyframe Selected");
        editor.Detail.IsEffectivelyVisible.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void AnOvershootingEasing_StaysInsideItsRow_UntilClampingIsTurnedOff()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel first = editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel last = editor.AddStateKeyframe($"{Category}/Released");
        editor.ClickAt(editor.KeyframeMarkerCenter(first));
        // Back/In dips below 0 early on, which draws below the row (above it would be clipped by
        // the timeline's scroll viewer for the first row).
        editor.DetailCombos[1].SelectedItem = InterpolationType.Back;
        editor.DetailCombos[2].SelectedItem = Easing.In;
        editor.Layout();
        first.InterpolationType.ShouldBe(InterpolationType.Back);
        Point start = editor.KeyframeMarkerCenter(first);
        Point end = editor.KeyframeMarkerCenter(last);
        double trackBottom = start.Y + editor.TimelineTrackHeight / 2;
        Point belowTheRow = new Point(start.X + (end.X - start.X) * 0.3, trackBottom + 4);

        // The thin antialiased line blends with the background, so look for anything bluish.
        static bool Bluish(Color pixel) => pixel.B > pixel.R + 40;

        editor.ViewModel.ClampInterpolationVisuals.ShouldBeTrue();
        editor.AnyPixelNear(belowTheRow, 2, Bluish).ShouldBeFalse("a clamped curve never leaves its row");

        editor.ViewModel.ClampInterpolationVisuals = false;
        editor.SaveFrame("overshoot-unclamped");

        editor.AnyPixelNear(belowTheRow, 3, Bluish).ShouldBeTrue("an unclamped Back easing dips below the row");
    }
}
