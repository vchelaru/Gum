using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Shouldly;
using WpfDataUi.Controls;

namespace Gum.Presentation.Tests.DataUi;

/// <summary>The angle, string-list, list, and multi-file logic both heads' editors share.</summary>
public class CompositeEditorLogicTests
{
    [Fact]
    public void AngleSelectorLogic_ConvertsBetweenShownDegreesAndTheWrittenUnit()
    {
        AngleSelectorLogic logic = new AngleSelectorLogic();

        logic.ToInstanceValue(90m, AngleType.Degrees).ShouldBe(90f);
        ((float)logic.ToInstanceValue(180m, AngleType.Radians)!).ShouldBe((float)System.Math.PI, 0.0001f);
        logic.ToInstanceValue(null, AngleType.Radians).ShouldBeNull();

        logic.TryGetDisplayedDegrees((float)System.Math.PI, AngleType.Radians, out float? degrees).ShouldBeTrue();
        degrees!.Value.ShouldBe(180f, 0.001f);
        logic.TryGetDisplayedDegrees("not an angle", AngleType.Degrees, out _).ShouldBeFalse();
    }

    [Fact]
    public void AngleSelectorLogic_TryParseAngleText_HandlesEmptyNumbersAndMath()
    {
        AngleSelectorLogic logic = new AngleSelectorLogic();

        logic.TryParseAngleText("", typeof(float), out float? empty).ShouldBeTrue();
        empty.ShouldBeNull();
        logic.TryParseAngleText("45", typeof(float), out float? number).ShouldBeTrue();
        number.ShouldBe(45f);
        logic.TryParseAngleText("30*3", typeof(float), out float? math).ShouldBeTrue();
        math.ShouldBe(90f);
    }

    [Fact]
    public void AngleSelectorLogic_DialDrag_JumpsToTheClickThenWindsPast180()
    {
        AngleSelectorLogic logic = new AngleSelectorLogic();
        logic.BeginDialDrag(0);

        // Straight up on screen is 90 degrees.
        logic.TryDragTo(0, -10, isShiftDown: false, snappingInterval: 1, out decimal up).ShouldBeTrue();
        up.ShouldBe(90m);

        // Continuing counter-clockwise past 180 keeps counting instead of wrapping to -179.
        logic.TryDragTo(-10, 0, isShiftDown: false, snappingInterval: 1, out decimal left).ShouldBeTrue();
        left.ShouldBe(180m);
        logic.TryDragTo(-10, 1, isShiftDown: false, snappingInterval: 1, out decimal pastLeft).ShouldBeTrue();
        pastLeft.ShouldBeGreaterThan(180m);

        logic.TryDragTo(0, 0, isShiftDown: false, snappingInterval: 1, out _).ShouldBeFalse();
    }

    [Fact]
    public void AngleSelectorLogic_ShiftSnapsToFifteenDegrees()
    {
        AngleSelectorLogic logic = new AngleSelectorLogic();
        logic.BeginDialDrag(0);

        // About 26 degrees snaps to 30.
        logic.TryDragTo(10, -5, isShiftDown: true, snappingInterval: 1, out decimal snapped).ShouldBeTrue();
        snapped.ShouldBe(30m);
    }

    [Fact]
    public void StringListLogic_ParsesTrimmedNonEmptyLines_AndFindsTheCaretsLine()
    {
        StringListLogic logic = new StringListLogic();

        logic.ParseLines(" a \r\n\r\nb\r\n").ShouldBe(new[] { "a", "b" });
        logic.ParseLines("x\ny").ShouldBe(new[] { "x", "y" });
        logic.ParseLines(null).ShouldBeEmpty();

        string text = "Width = A.Width\nHeight = B.Height";
        logic.GetLineAt(text, 3).ShouldBe("Width = A.Width");
        logic.GetLineAt(text, text.Length).ShouldBe("Height = B.Height");
        logic.GetLineAt("", 0).ShouldBe("");
    }

    [Fact]
    public void ListBoxDisplayLogic_CopiesReadsAndEditsTypedLists()
    {
        ListBoxDisplayLogic logic = new ListBoxDisplayLogic();
        List<int> original = new List<int> { 1, 2 };

        IList copy = logic.CreateEditableCopy(original, typeof(List<int>))!;
        copy.ShouldNotBeSameAs(original);

        logic.AddOrReplace(copy, indexEditing: null, "3").ShouldBeNull();
        logic.AddOrReplace(copy, indexEditing: 0, "9").ShouldBeNull();
        logic.AddOrReplace(copy, indexEditing: null, "not a number").ShouldBeNull();

        logic.TryReadList(copy, typeof(List<int>), out object? read).ShouldBeTrue();
        read.ShouldBe(new List<int> { 9, 2, 3 });
        original.ShouldBe(new List<int> { 1, 2 });
    }

    [Fact]
    public void ListBoxDisplayLogic_Vector2_ParsesBracketedPairs_AndReportsBadInput()
    {
        ListBoxDisplayLogic logic = new ListBoxDisplayLogic();
        IList vectors = logic.CreateEditableCopy(null, typeof(List<Vector2>))!;

        logic.AddOrReplace(vectors, null, "<10,20>").ShouldBeNull();
        logic.AddOrReplace(vectors, null, "10").ShouldBe(ListBoxDisplayLogic.Vector2ParseError);

        vectors.Cast<Vector2>().ShouldBe(new[] { new Vector2(10, 20) });
        logic.TryReadList(new object[] { "nope" }, typeof(List<string>), out object? strings).ShouldBeTrue();
        logic.TryReadList(new object[] { 1 }, typeof(Dictionary<int, int>), out _).ShouldBeFalse();
    }

    [Fact]
    public void MultiFileDisplayLogic_RemovesAndMovesWithinBounds()
    {
        MultiFileDisplayLogic logic = new MultiFileDisplayLogic();
        logic.SetEntries(new List<string> { "a.csv", "b.csv", "c.csv" });

        logic.Move(0, -1, out _).ShouldBeFalse();
        logic.Move(0, 1, out int newIndex).ShouldBeTrue();
        newIndex.ShouldBe(1);
        logic.Entries.ShouldBe(new[] { "b.csv", "a.csv", "c.csv" });

        logic.RemoveAt(5).ShouldBeFalse();
        logic.RemoveAt(2).ShouldBeTrue();
        logic.GetValue().ShouldBe(new List<string> { "b.csv", "a.csv" });

        logic.IsRemoveVisible(-1).ShouldBeFalse();
        logic.IsMoveVisible(0).ShouldBeTrue();
        logic.SetEntries(null);
        logic.IsMoveVisible(0).ShouldBeFalse();
    }
}
