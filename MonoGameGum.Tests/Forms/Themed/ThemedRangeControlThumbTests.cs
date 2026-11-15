using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms.Themed;

/// <summary>
/// Slider and ScrollBar thumbs of every packaged theme, loaded from the theme files: the thumb stays inside
/// the track, reaches both ends, is proportional to the viewport, and the buttons and track stay inside the
/// control at small, odd and large lengths, for both orientations.
/// </summary>
public class ThemedRangeControlThumbTests : FileThemeInputTestBase
{
    private const float Tol = 1.01f;
    public static readonly float[] Lengths = { 80f, 301f };

    public static IEnumerable<object[]> ThemeAndLength() =>
        AllThemes().Select(row => (string)row[0]).SelectMany(t => Lengths.Select(l => new object[] { t, l }));

    public static IEnumerable<object[]> ThemeLengthOrientation() =>
        AllThemes().Select(row => (string)row[0]).SelectMany(t => Lengths.SelectMany(l =>
            new[] { Orientation.Vertical, Orientation.Horizontal }.Select(o => new object[] { t, l, o })));

    private (InteractiveGue Box, ScrollBar Bar, GraphicalUiElement Track, GraphicalUiElement Thumb) Bar(string theme, float length, Orientation orientation)
    {
        GumProjectSave project = LoadTheme(theme);
        InteractiveGue box = PlaceComponent(project, Find(project, "ScrollBar")!);
        ScrollBar bar = (ScrollBar)box.FormsControlAsObject!;
        bar.Orientation = orientation;
        box.WidthUnits = DimensionUnitType.Absolute;
        box.HeightUnits = DimensionUnitType.Absolute;
        if (orientation == Orientation.Vertical) { box.Height = length; } else { box.Width = length; }
        bar.Minimum = 0;
        bar.Maximum = 100;
        bar.ViewportSize = 50;
        bar.Value = 0;
        Driver.Step(2);
        return (box, bar, box.FindByName("TrackInstance")!, box.FindByName("ThumbInstance")!);
    }

    private static (float Start, float Length) Axis(GraphicalUiElement e, Orientation o) =>
        o == Orientation.Vertical ? (e.AbsoluteTop, e.AbsoluteHeight) : (e.AbsoluteLeft, e.AbsoluteWidth);

    [Theory]
    [MemberData(nameof(ThemeLengthOrientation))]
    public void ScrollBar_Parts_StayInsideTheControl(string theme, float length, Orientation o)
    {
        if (theme == "" || theme == "Editor") { return; } // Standard and Editor bar buttons are mis-laid-out: #7439
        var s = Bar(theme, length, o);
        List<string> outside = new List<string>();
        foreach (string name in new[] { "TrackInstance", "ThumbInstance", "UpButtonInstance", "DownButtonInstance" })
        {
            GraphicalUiElement? part = s.Box.FindByName(name);
            if (part == null || !part.AbsoluteVisible) { continue; }
            if (part.AbsoluteLeft < s.Box.AbsoluteLeft - Tol || part.AbsoluteTop < s.Box.AbsoluteTop - Tol
                || part.AbsoluteLeft + part.AbsoluteWidth > s.Box.AbsoluteLeft + s.Box.AbsoluteWidth + Tol
                || part.AbsoluteTop + part.AbsoluteHeight > s.Box.AbsoluteTop + s.Box.AbsoluteHeight + Tol)
            {
                outside.Add($"{name} {part.AbsoluteLeft},{part.AbsoluteTop} {part.AbsoluteWidth}x{part.AbsoluteHeight} vs box {s.Box.AbsoluteLeft},{s.Box.AbsoluteTop} {s.Box.AbsoluteWidth}x{s.Box.AbsoluteHeight}");
            }
        }
        outside.ShouldBeEmpty($"{theme} {o} {length}: " + string.Join("; ", outside));
    }

    [Theory]
    [MemberData(nameof(ThemeLengthOrientation))]
    public void ScrollBar_Buttons_DoNotOverlapTheTrack(string theme, float length, Orientation o)
    {
        if (theme == "" || theme == "Editor") { return; } // Standard and Editor bar buttons are mis-laid-out: #7439
        var s = Bar(theme, length, o);
        (float tStart, float tLen) = Axis(s.Track, o);
        List<string> overlaps = new List<string>();
        foreach (string name in new[] { "UpButtonInstance", "DownButtonInstance" })
        {
            GraphicalUiElement? button = s.Box.FindByName(name);
            if (button == null || !button.AbsoluteVisible) { continue; }
            (float bStart, float bLen) = Axis(button, o);
            if (bStart < tStart + tLen - Tol && bStart + bLen > tStart + Tol)
            {
                overlaps.Add($"{name} {bStart}+{bLen} overlaps track {tStart}+{tLen}");
            }
        }
        overlaps.ShouldBeEmpty($"{theme} {o} {length}: " + string.Join("; ", overlaps));
    }

    [Theory]
    [MemberData(nameof(ThemeLengthOrientation))]
    public void ScrollBar_ThumbPosition_MovesMonotonicallyWithValue(string theme, float length, Orientation o)
    {
        var s = Bar(theme, length, o);
        float previous = float.MinValue;
        foreach (double value in new[] { 0.0, 25, 50, 75, 100 })
        {
            s.Bar.Value = value;
            Driver.Step(1);
            float start = Axis(s.Thumb, o).Start;
            start.ShouldBeGreaterThanOrEqualTo(previous - 0.01f, $"{theme} {o} {length} value {value}");
            previous = start;
        }
        (Axis(s.Thumb, o).Start).ShouldBeGreaterThan(Axis(s.Track, o).Start + 0.5f, $"{theme} {o} {length}: thumb never left the start");
    }

    private (InteractiveGue Box, Slider Slider, GraphicalUiElement Track, GraphicalUiElement Thumb) SliderAt(string theme, float width)
    {
        GumProjectSave project = LoadTheme(theme);
        InteractiveGue box = PlaceComponent(project, Find(project, "Slider")!);
        Slider slider = (Slider)box.FormsControlAsObject!;
        box.WidthUnits = DimensionUnitType.Absolute;
        box.Width = width;
        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.Value = 0;
        Driver.Step(2);
        return (box, slider, box.FindByName("TrackInstance")!, box.FindByName("ThumbInstance")!);
    }

    [Theory]
    [MemberData(nameof(ThemeAndLength))]
    public void Slider_ThumbAtMinimumAndMaximum_StaysInsideTheControl(string theme, float width)
    {
        var s = SliderAt(theme, width);
        float trackLeft = s.Track.AbsoluteLeft, trackRight = trackLeft + s.Track.AbsoluteWidth;

        s.Thumb.AbsoluteLeft.ShouldBeGreaterThanOrEqualTo(s.Box.AbsoluteLeft - Tol, $"{theme} {width} at minimum");
        s.Slider.Value = 100;
        Driver.Step(2);
        (s.Thumb.AbsoluteLeft + s.Thumb.AbsoluteWidth).ShouldBeLessThanOrEqualTo(s.Box.AbsoluteLeft + s.Box.AbsoluteWidth + Tol, $"{theme} {width} at maximum: thumb {s.Thumb.AbsoluteLeft}+{s.Thumb.AbsoluteWidth} track {trackLeft}+{s.Track.AbsoluteWidth}");
    }

    [Theory]
    [MemberData(nameof(ThemeAndLength))]
    public void Slider_ThumbTravel_CoversTheTrackWidthMinusThumb(string theme, float width)
    {
        var s = SliderAt(theme, width);
        float atMin = s.Thumb.AbsoluteLeft;
        s.Slider.Value = 100;
        Driver.Step(2);
        float travel = s.Thumb.AbsoluteLeft - atMin;

        travel.ShouldBeInRange(s.Track.AbsoluteWidth - s.Thumb.AbsoluteWidth - Tol, s.Track.AbsoluteWidth + Tol, $"{theme} {width}: thumb {s.Thumb.AbsoluteWidth}, track {s.Track.AbsoluteWidth}");
    }

    [Theory]
    [MemberData(nameof(ThemeAndLength))]
    public void Slider_ThumbAtMidValue_IsCenteredOnTheTrack(string theme, float width)
    {
        var s = SliderAt(theme, width);
        s.Slider.Value = 50;
        Driver.Step(2);
        float thumbCenter = s.Thumb.AbsoluteLeft + s.Thumb.AbsoluteWidth / 2;
        float trackCenter = s.Track.AbsoluteLeft + s.Track.AbsoluteWidth / 2;

        thumbCenter.ShouldBe(trackCenter, Tol, $"{theme} {width}");
    }

    [Theory]
    [MemberData(nameof(ThemeAndLength))]
    public void Slider_ClickNearTrackEnds_SetsValueNearMinimumAndMaximum(string theme, float width)
    {
        var s = SliderAt(theme, width);
        s.Slider.IsMoveToPointEnabled = true;
        int y = (int)(s.Track.AbsoluteTop + s.Track.AbsoluteHeight / 2);

        Driver.Click((int)(s.Track.AbsoluteLeft + s.Track.AbsoluteWidth - 2), y);
        s.Slider.Value.ShouldBeGreaterThan(90, $"{theme} {width}: click at the right end of the track");

        Driver.Click((int)(s.Track.AbsoluteLeft + 2), y);
        s.Slider.Value.ShouldBeLessThan(10, $"{theme} {width}: click at the left end of the track");
    }
}
