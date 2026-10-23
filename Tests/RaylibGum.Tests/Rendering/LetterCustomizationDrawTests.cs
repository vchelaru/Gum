using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using static RaylibGum.Tests.Rendering.TextInkRender;
using LetterCustomization = Gum.Renderables.LetterCustomization;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #6550: a [Custom] letter's ScaleX/ScaleY/RotationDegrees change only how it is drawn, as on MonoGame and
/// Skia: it is scaled about the cell point ScaleXOrigin/ScaleYOrigin name (Center by default) with both
/// scales, turned counterclockwise about the scaled cell's top-left, and the next letter does not move.
/// </summary>
public class LetterCustomizationDrawTests : BaseTestClass
{
    [Fact]
    public void Draw_CustomTagWithNoTransform_DrawsSamePixelsAsPlainText()
    {
        Text.Customizations["NoTransform"] = (index, block) => new LetterCustomization();

        Color[] plain = Render("I", left: 80, top: 80);
        Color[] customized = Render("[Custom=NoTransform]I[/Custom]", left: 80, top: 80);

        customized.SequenceEqual(plain).ShouldBeTrue();
    }

    [Fact]
    public void Draw_CustomTagNonUniformScale_ScalesEachAxisAboutTheCellCenter()
    {
        Text.Customizations["Wide"] = (index, block) => new LetterCustomization
        {
            ScaleX = 3,
            ScaleY = 1,
        };

        Bounds plain = InkBounds(Render("I", left: 80, top: 80));
        Bounds scaled = InkBounds(Render("[Custom=Wide]I[/Custom]", left: 80, top: 80));

        scaled.Width.ShouldBe(3 * plain.Width, tolerance: 3);
        scaled.Height.ShouldBe(plain.Height, tolerance: 1);
        scaled.Top.ShouldBe(plain.Top, tolerance: 1);
    }

    [Fact]
    public void Draw_CustomTagScale_KeepsTheCellCenterFixedByDefault()
    {
        Text.Customizations["Double"] = (index, block) => new LetterCustomization
        {
            ScaleX = 2,
            ScaleY = 2,
        };

        Text plainText = BuildText("I", 80, 80, out TextRuntime runtime);
        float cellWidth = plainText.WrappedTextWidth;
        float cellHeight = plainText.WrappedTextHeight;
        Gum.GumService.Default.Root.Children!.Remove(runtime);
        float centerX = 80 + cellWidth / 2;
        float centerY = 80 + cellHeight / 2;

        Bounds plain = InkBounds(Render("I", left: 80, top: 80));
        Bounds scaled = InkBounds(Render("[Custom=Double]I[/Custom]", left: 80, top: 80));

        // Scaling about the cell center doubles every ink edge's distance from it.
        (scaled.Left - centerX).ShouldBe(2 * (plain.Left - centerX), tolerance: 2);
        (scaled.Right - centerX).ShouldBe(2 * (plain.Right - centerX), tolerance: 2);
        (scaled.Top - centerY).ShouldBe(2 * (plain.Top - centerY), tolerance: 2);
        (scaled.Bottom - centerY).ShouldBe(2 * (plain.Bottom - centerY), tolerance: 2);
    }

    [Fact]
    public void Draw_CustomTagScaleWithRightBottomOrigin_KeepsTheCellBottomRightFixed()
    {
        Text.Customizations["DoubleFromBottomRight"] = (index, block) => new LetterCustomization
        {
            ScaleX = 2,
            ScaleY = 2,
            ScaleXOrigin = HorizontalAlignment.Right,
            ScaleYOrigin = VerticalAlignment.Bottom,
        };

        Text plainText = BuildText("I", 80, 80, out TextRuntime runtime);
        float right = 80 + plainText.WrappedTextWidth;
        float bottom = 80 + plainText.WrappedTextHeight;
        Gum.GumService.Default.Root.Children!.Remove(runtime);

        Bounds plain = InkBounds(Render("I", left: 80, top: 80));
        Bounds scaled = InkBounds(Render("[Custom=DoubleFromBottomRight]I[/Custom]", left: 80, top: 80));

        (scaled.Left - right).ShouldBe(2 * (plain.Left - right), tolerance: 2);
        (scaled.Right - right).ShouldBe(2 * (plain.Right - right), tolerance: 2);
        (scaled.Top - bottom).ShouldBe(2 * (plain.Top - bottom), tolerance: 2);
        (scaled.Bottom - bottom).ShouldBe(2 * (plain.Bottom - bottom), tolerance: 2);
    }

    [Fact]
    public void Draw_CustomTagRotation_RotatesTheLetterCounterclockwiseAboutTheCellTopLeft()
    {
        Text.Customizations["Quarter"] = (index, block) => new LetterCustomization
        {
            RotationDegrees = 90,
        };

        Bounds plain = InkBounds(Render("I", left: 80, top: 80));
        Bounds rotated = InkBounds(Render("[Custom=Quarter]I[/Custom]", left: 80, top: 80));

        // A counterclockwise quarter turn about (80, 80) maps (dx, dy) to (dy, -dx): the letter lies above it.
        rotated.Left.ShouldBe(80 + (plain.Top - 80), tolerance: 2);
        rotated.Right.ShouldBe(80 + (plain.Bottom - 80), tolerance: 2);
        rotated.Top.ShouldBe(80 - (plain.Right - 80), tolerance: 2);
        rotated.Bottom.ShouldBe(80 - (plain.Left - 80), tolerance: 2);
    }

    [Fact]
    public void Draw_CustomTagScaleAndRotation_DoesNotMoveTheFollowingLetter()
    {
        Text.Customizations["SmallTurn"] = (index, block) => new LetterCustomization
        {
            ScaleX = 0.5f,
            ScaleY = 0.5f,
            RotationDegrees = 10,
        };

        Bounds plain = InkBounds(Render("IW", left: 20, top: 80));
        Bounds customized = InkBounds(Render("[Custom=SmallTurn]I[/Custom]W", left: 20, top: 80));

        // The W is the rightmost ink in both, so it is drawn at the same place.
        customized.Right.ShouldBe(plain.Right);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(90f)]
    public void Draw_CustomTagTransformAtSubpixelPosition_SnapsToTheNearestPixel(float rotationDegrees)
    {
        // #6554: MonoGame snaps a scaled glyph's position, and a rotated one's when the turn is a whole
        // quarter (SpriteRenderer.ShouldSnapPosition), so it lands where it would at the rounded position.
        Text.Customizations["SnapScaled"] = (index, block) => new LetterCustomization
        {
            ScaleX = 1.25f,
            ScaleY = 1.25f,
            ScaleXOrigin = HorizontalAlignment.Left,
            ScaleYOrigin = VerticalAlignment.Top,
            RotationDegrees = rotationDegrees,
        };

        Color[] whole = Render("[Custom=SnapScaled]I[/Custom]", left: 80, top: 80);
        Color[] subpixel = Render("[Custom=SnapScaled]I[/Custom]", left: 80.3f, top: 80.3f);

        subpixel.SequenceEqual(whole).ShouldBeTrue();
    }

    [Fact]
    public void Draw_CustomTagRotationOffAQuarterTurn_IsNotSnapped()
    {
        // #6554: MonoGame draws a glyph turned by anything but a whole quarter where it falls, unsnapped.
        Text.Customizations["Tilted"] = (index, block) => new LetterCustomization
        {
            ScaleX = 1.5f,
            ScaleY = 1.5f,
            RotationDegrees = 30,
        };

        Color[] whole = Render("[Custom=Tilted]I[/Custom]", left: 80, top: 80);
        Color[] subpixel = Render("[Custom=Tilted]I[/Custom]", left: 80.5f, top: 80.5f);

        subpixel.SequenceEqual(whole).ShouldBeFalse();
    }

    private static Text BuildText(string markup, float left, float top, out TextRuntime runtime)
    {
        runtime = new TextRuntime();
        runtime.X = left;
        runtime.Y = top;
        runtime.FontScale = 2;
        runtime.Text = markup;
        Gum.GumService.Default.Root.Children!.Add(runtime);
        Gum.GumService.Default.Root.UpdateLayout();
        return (Text)runtime.RenderableComponent!;
    }
}
