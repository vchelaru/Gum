using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Gum.DataTypes;
using Shouldly;
using StateAnimationPlugin.Timeline;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// The timeline as a control: what it draws (checked by reading the rendered pixels) and how
/// clicks and hovers on and around its markers land.
/// </summary>
public class TimelineInteractionTests
{
    private const string Category = "AnimationCategory";
    private static readonly Color DeselectedMarker = Color.Parse("#2a5c8a");
    private static readonly Color SelectedMarker = Color.Parse("#8cc8f5");
    private static readonly Color NowLine = Color.Parse("#c3e3ed");

    [AvaloniaFact]
    public void AnAnimationWithOneKeyframe_StillDrawsItsMarker_AndTheMarkerCanBeClicked()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel only = editor.AddStateKeyframe($"{Category}/Pressed");
        walk.SelectedKeyframe = null;
        editor.Layout();

        walk.Length.ShouldBe(0f);
        Point marker = editor.KeyframeMarkerCenter(only);
        editor.AnyPixelNear(marker, 4, pixel => IsClose(pixel, DeselectedMarker) || IsClose(pixel, SelectedMarker))
            .ShouldBeTrue("the only keyframe must be drawn even though the animation has no length");

        editor.ClickAt(marker);

        walk.SelectedKeyframe.ShouldBeSameAs(only);
        editor.AnyPixelNear(marker, 4, SelectedMarker).ShouldBeTrue();
        editor.SaveFrame("single-keyframe");
    }

    private static bool IsClose(Color pixel, Color color, int tolerance = 12) =>
        Math.Abs(pixel.R - color.R) <= tolerance && Math.Abs(pixel.G - color.G) <= tolerance && Math.Abs(pixel.B - color.B) <= tolerance;

    [AvaloniaFact]
    public void ClickingTheEmptyTrack_LeavesTheSelectionAlone()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel first = editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel second = editor.AddStateKeyframe($"{Category}/Released");
        editor.ClickAt(editor.KeyframeMarkerCenter(first));
        walk.SelectedKeyframe.ShouldBeSameAs(first);
        Point firstMarker = editor.KeyframeMarkerCenter(first);
        Point secondMarker = editor.KeyframeMarkerCenter(second);

        editor.ClickAt(new Point((firstMarker.X + secondMarker.X) / 2, firstMarker.Y));

        walk.SelectedKeyframe.ShouldBeSameAs(first);
    }

    [AvaloniaFact]
    public void ClickingJustOutsideAMarker_DoesNotSelectIt_ButAClickOnItsEdgeDoes()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel second = editor.AddStateKeyframe($"{Category}/Released");
        editor.AddStateKeyframe($"{Category}/Pressed");
        walk.SelectedKeyframe = null;
        editor.Layout();
        Point marker = editor.KeyframeMarkerCenter(second);
        double halfSize = editor.TimelineTrackHeight * 0.6 / 2;

        // Markers accept a click up to two pixels past their box.
        editor.ClickAt(new Point(marker.X + halfSize + 6, marker.Y));
        walk.SelectedKeyframe.ShouldBeNull();

        editor.ClickAt(new Point(marker.X + halfSize + 1, marker.Y));
        walk.SelectedKeyframe.ShouldBeSameAs(second);
    }

    [AvaloniaFact]
    public void TwoKeyframesAtTheSameTime_AreBothListed_AndAClickPicksTheLaterOne()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel first = editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel second = editor.AddStateKeyframe($"{Category}/Released");
        editor.ClickAt(editor.KeyframeMarkerCenter(second));
        editor.TypeAndEnter(editor.DetailTimeBox, "0");
        walk.SelectedKeyframe = null;
        editor.Layout();

        second.Time.ShouldBe(0f);
        editor.KeyframeList.Items.Count.ShouldBe(2);
        editor.Timeline.Rows[0].Items.ShouldBe(new[] { first, second });

        editor.ClickAt(editor.KeyframeMarkerCenter(second));

        walk.SelectedKeyframe.ShouldBeSameAs(second);
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single().States.Select(state => state.Time).ShouldBe(new[] { 0f, 0f });
    }

    [AvaloniaFact]
    public void TheCurrentTimeLine_IsDrawnWhereTheScrubberIs()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel first = editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel last = editor.AddStateKeyframe($"{Category}/Released");
        Point start = editor.KeyframeMarkerCenter(first);
        Point end = editor.KeyframeMarkerCenter(last);
        Point quarter = new Point(start.X + (end.X - start.X) * 0.25, start.Y);
        editor.AnyPixelNear(quarter, 2, NowLine).ShouldBeFalse();

        editor.TypeAndEnter(editor.TimelineTimeBox, "0.25");

        editor.AnyPixelNear(quarter, 2, NowLine).ShouldBeTrue();
        editor.AnyPixelNear(new Point(start.X + (end.X - start.X) * 0.75, start.Y), 2, NowLine).ShouldBeFalse();
    }

    [AvaloniaFact]
    public void HoveringAMarker_ShowsItsStateAsTheTooltip()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel released = editor.AddStateKeyframe($"{Category}/Released");

        editor.Hover(editor.KeyframeMarkerCenter(released));

        editor.TimelineTrackTipFor(released).ShouldBe($"{Category}/Released");
        editor.Hover(new Point(5, 5));
        editor.TimelineTrackTipFor(released).ShouldBeNull();
    }
}
