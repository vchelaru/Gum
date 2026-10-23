using Raylib_cs;
using Shouldly;
using static RaylibGum.Tests.Rendering.TextInkRender;
using LetterCustomization = Gum.Renderables.LetterCustomization;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #6553: Text.Rotation turns the whole text block counterclockwise about the Text's top-left, as on MonoGame
/// and Skia. Styled runs and [Custom] letters turn with the block, as on Skia.
/// </summary>
public class TextRotationDrawTests : BaseTestClass
{
    [Fact]
    public void Draw_Rotation90_TurnsPlainTextCounterclockwiseAboutTheTopLeft()
    {
        Bounds plain = InkBounds(Render("IW", left: 80, top: 120));
        Bounds rotated = InkBounds(Render("IW", left: 80, top: 120, text => text.Rotation = 90));

        // A counterclockwise quarter turn about (80, 120) maps (dx, dy) to (dy, -dx): the text lies above it.
        rotated.Left.ShouldBe(80 + (plain.Top - 120), tolerance: 2);
        rotated.Right.ShouldBe(80 + (plain.Bottom - 120), tolerance: 2);
        rotated.Top.ShouldBe(120 - (plain.Right - 80), tolerance: 2);
        rotated.Bottom.ShouldBe(120 - (plain.Left - 80), tolerance: 2);
    }

    [Fact]
    public void Draw_Rotation180_TurnsStyledRunsWithTheBlock()
    {
        // Each run advances along the turned baseline, so the styled text covers what the plain text does.
        Bounds plain = InkBounds(Render("IW", left: 160, top: 120));
        Bounds rotatedPlain = InkBounds(Render("IW", left: 160, top: 120, text => text.Rotation = 180));
        Bounds rotatedStyled = InkBounds(Render("[Color=Red]I[/Color]W", left: 160, top: 120, text => text.Rotation = 180));

        rotatedPlain.Left.ShouldBe(160 - (plain.Right - 160), tolerance: 2);
        rotatedPlain.Top.ShouldBe(120 - (plain.Bottom - 120), tolerance: 2);
        rotatedStyled.ShouldBe(rotatedPlain);
    }

    [Fact]
    public void Draw_Rotation90WithCustomLetterTurn_AddsTheTurns()
    {
        Text.Customizations["QuarterTurn"] = (index, block) => new LetterCustomization
        {
            RotationDegrees = 90,
        };

        Bounds plain = InkBounds(Render("I", left: 120, top: 120));
        Bounds turned = InkBounds(Render("[Custom=QuarterTurn]I[/Custom]", left: 120, top: 120, text => text.Rotation = 90));

        // The letter's cell top-left is the Text's top-left, so the two quarter turns make a half turn about it.
        turned.Left.ShouldBe(120 - (plain.Right - 120), tolerance: 2);
        turned.Right.ShouldBe(120 - (plain.Left - 120), tolerance: 2);
        turned.Top.ShouldBe(120 - (plain.Bottom - 120), tolerance: 2);
        turned.Bottom.ShouldBe(120 - (plain.Top - 120), tolerance: 2);
    }

    [Fact]
    public void Draw_Rotation30_IsNotSnapped()
    {
        // MonoGame draws text turned by anything but a whole quarter where it falls, unsnapped.
        Color[] whole = Render("IW", left: 80, top: 120, text => text.Rotation = 30);
        Color[] subpixel = Render("IW", left: 80.3f, top: 120.3f, text => text.Rotation = 30);

        subpixel.SequenceEqual(whole).ShouldBeFalse();
    }

    [Fact]
    public void Draw_Rotation90AtSubpixelPosition_SnapsToTheNearestPixel()
    {
        Color[] whole = Render("IW", left: 80, top: 120, text => text.Rotation = 90);
        Color[] subpixel = Render("IW", left: 80.3f, top: 120.3f, text => text.Rotation = 90);

        subpixel.SequenceEqual(whole).ShouldBeTrue();
    }
}
