using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Changing a property that alters the text's measured size (FontScale, LineHeightMultiplier, FontSize) lays
/// out like a fresh element with that value, and changing it back returns to the original layout, for a
/// RelativeToChildren width with an offset, in every X origin, wrapped and unwrapped, with Visible and
/// suspension around the change.
/// </summary>
public class TextMeasureAffectingPropertyLayoutTests : BaseTestClass
{
    record struct Rect(float Left, float Top, float Width, float Height);

    const string LongText = "Mason went to the market to buy some bread and cheese";

    static Rect Snapshot(GraphicalUiElement e) => new(e.AbsoluteLeft, e.AbsoluteTop, e.AbsoluteWidth, e.AbsoluteHeight);

    static readonly (string Name, Action<TextRuntime> Change, Action<TextRuntime> Restore)[] Properties =
    {
        ("FontScale", t => t.FontScale = 2, t => t.FontScale = 1),
        ("LineHeightMultiplier", t => t.LineHeightMultiplier = 2, t => t.LineHeightMultiplier = 1),
        ("FontSize", t => t.FontSize = 40, t => t.FontSize = 18),
    };

    public static IEnumerable<object[]> Cases()
    {
        float[] offsets = { 0, 4, -4 };
        HorizontalAlignment[] xOrigins = { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right };
        for (int p = 0; p < Properties.Length; p++)
            foreach (float offset in offsets)
                foreach (HorizontalAlignment x in xOrigins)
                    foreach (bool wrapped in new[] { false, true })
                        yield return new object[] { p, offset, x, wrapped };
    }

    static (TextRuntime text, ContainerRuntime parent) Build(float offset, HorizontalAlignment xOrigin, bool wrapped)
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        parent.Height = 0;
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = offset;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Height = 0;
        text.XOrigin = xOrigin;
        text.X = 20;
        if (wrapped) text.MaxWidth = 80;
        parent.AddChild(text);
        text.Text = wrapped ? LongText : "Mason";
        return (text, parent);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Property_ShouldMatchFreshLayout_WhenChanged(int p, float offset, HorizontalAlignment x, bool wrapped)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(offset, x, wrapped);

        Properties[p].Change(text);
        (TextRuntime valueFirst, ContainerRuntime valueFirstParent) = Build(offset, x, wrapped);
        valueFirst.Text = "";
        Properties[p].Change(valueFirst);
        valueFirst.Text = wrapped ? LongText : "Mason";

        Snapshot(text).ShouldBe(Snapshot(valueFirst), Properties[p].Name);
        Snapshot(parent).ShouldBe(Snapshot(valueFirstParent), Properties[p].Name);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Property_ShouldRestoreLayout_WhenChangedAndChangedBack(int p, float offset, HorizontalAlignment x, bool wrapped)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(offset, x, wrapped);
        Rect textBefore = Snapshot(text);
        Rect parentBefore = Snapshot(parent);

        Properties[p].Change(text);
        Properties[p].Restore(text);

        Snapshot(text).ShouldBe(textBefore, Properties[p].Name);
        Snapshot(parent).ShouldBe(parentBefore, Properties[p].Name);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Property_ShouldKeepLayout_WhenChangedWhileHiddenOrSuspended(int p, float offset, HorizontalAlignment x, bool wrapped)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(offset, x, wrapped);
        (TextRuntime reference, ContainerRuntime referenceParent) = Build(offset, x, wrapped);
        Properties[p].Change(reference);

        text.Visible = false;
        parent.SuspendLayout();
        Properties[p].Change(text);
        parent.ResumeLayout();
        text.Visible = true;

        Snapshot(text).ShouldBe(Snapshot(reference), Properties[p].Name);
        Snapshot(parent).ShouldBe(Snapshot(referenceParent), Properties[p].Name);
    }
}
