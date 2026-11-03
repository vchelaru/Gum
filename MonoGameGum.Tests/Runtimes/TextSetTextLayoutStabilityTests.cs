using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Assigning Text must leave the layout exactly where a fresh layout of that text would put it, for every
/// width unit, offset and origin, alone or in a RelativeToChildren / stacking parent. (#7550: a
/// RelativeToChildren text lost its Width offset when the new text measured the same size.)
/// </summary>
public class TextSetTextLayoutStabilityTests : BaseTestClass
{
    record struct Rect(float Left, float Top, float Width, float Height);

    static Rect Snapshot(GraphicalUiElement element) =>
        new(element.AbsoluteLeft, element.AbsoluteTop, element.AbsoluteWidth, element.AbsoluteHeight);

    public static IEnumerable<object[]> TextConfigurations()
    {
        DimensionUnitType[] widthUnits =
        {
            DimensionUnitType.Absolute, DimensionUnitType.RelativeToChildren, DimensionUnitType.RelativeToParent,
            DimensionUnitType.PercentageOfParent, DimensionUnitType.Ratio, DimensionUnitType.ScreenPixel
        };
        float[] offsets = { 0, 4, -3 };
        HorizontalAlignment[] xOrigins = { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right };
        foreach (DimensionUnitType units in widthUnits)
        {
            foreach (float offset in offsets)
            {
                foreach (HorizontalAlignment xOrigin in xOrigins)
                {
                    yield return new object[] { units, offset, xOrigin, false };
                    yield return new object[] { units, offset, xOrigin, true };
                }
            }
        }
    }

    static (TextRuntime text, GraphicalUiElement root) Build(DimensionUnitType widthUnits, float width,
        HorizontalAlignment xOrigin, bool insideRelativeToChildrenParent)
    {
        TextRuntime text = new();
        text.WidthUnits = widthUnits;
        text.Width = width;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Height = 0;
        text.XOrigin = xOrigin;
        text.X = 30;
        text.Y = 5;

        if (insideRelativeToChildrenParent)
        {
            ContainerRuntime parent = new();
            parent.WidthUnits = DimensionUnitType.RelativeToChildren;
            parent.HeightUnits = DimensionUnitType.RelativeToChildren;
            parent.Width = 0;
            parent.Height = 0;
            parent.AddChild(text);
            return (text, parent);
        }

        ContainerRuntime fixedParent = new();
        fixedParent.Width = 300;
        fixedParent.Height = 200;
        fixedParent.AddChild(text);
        return (text, fixedParent);
    }

    [Theory]
    [MemberData(nameof(TextConfigurations))]
    public void Text_ShouldKeepLayout_WhenSetToSameString(DimensionUnitType widthUnits, float width,
        HorizontalAlignment xOrigin, bool insideRelativeToChildrenParent)
    {
        (TextRuntime text, GraphicalUiElement root) = Build(widthUnits, width, xOrigin, insideRelativeToChildrenParent);
        text.Text = "Mason";
        Rect textBefore = Snapshot(text);
        Rect rootBefore = Snapshot(root);

        text.Text = "Mason";

        Snapshot(text).ShouldBe(textBefore);
        Snapshot(root).ShouldBe(rootBefore);
    }

    [Theory]
    [MemberData(nameof(TextConfigurations))]
    public void Text_ShouldMatchFreshLayout_WhenSetToSameSizeDifferentString(DimensionUnitType widthUnits, float width,
        HorizontalAlignment xOrigin, bool insideRelativeToChildrenParent)
    {
        // Identical glyph advances in the default font, so the measured size is the same.
        (TextRuntime text, GraphicalUiElement root) = Build(widthUnits, width, xOrigin, insideRelativeToChildrenParent);
        text.Text = "aaaaa";
        text.Text = "bbbbb";
        text.Width.ShouldBe(width);

        (TextRuntime fresh, GraphicalUiElement freshRoot) = Build(widthUnits, width, xOrigin, insideRelativeToChildrenParent);
        fresh.Text = "bbbbb";

        Snapshot(text).Width.ShouldBe(Snapshot(fresh).Width, 0.01f);
        Snapshot(root).Width.ShouldBe(Snapshot(freshRoot).Width, 0.01f);
    }

    [Theory]
    [MemberData(nameof(TextConfigurations))]
    public void Text_ShouldMatchFreshLayout_AfterGrowingThenShrinking(DimensionUnitType widthUnits, float width,
        HorizontalAlignment xOrigin, bool insideRelativeToChildrenParent)
    {
        (TextRuntime text, GraphicalUiElement root) = Build(widthUnits, width, xOrigin, insideRelativeToChildrenParent);
        text.Text = "Mason";
        text.Text = "A much longer line of text";
        text.Text = "Mason";
        text.Text = "Mason";

        (TextRuntime fresh, GraphicalUiElement freshRoot) = Build(widthUnits, width, xOrigin, insideRelativeToChildrenParent);
        fresh.Text = "Mason";

        Snapshot(text).ShouldBe(Snapshot(fresh));
        Snapshot(root).ShouldBe(Snapshot(freshRoot));
    }
}
