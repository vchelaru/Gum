using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #2852: when Width != Height the Apos.Shapes Circle previously sized its radius from
// Width alone, which produced two divergent results vs Skia (the tool/viewport renderer):
// the circle could overflow the box on the short axis, and a wide-but-short circle's center
// landed below the box because center.Y was also derived from Width. These tests pin the
// canonical behavior to "fit inside the bounding box, centered" — matching Skia.
public class CircleRenderableTests
{
    [Fact]
    public void Radius_WhenWidthGreaterThanHeight_UsesSmallerDimension()
    {
        Circle sut = new() { Width = 200, Height = 50 };

        sut.Radius.ShouldBe(25f);
    }

    [Fact]
    public void Radius_WhenHeightGreaterThanWidth_UsesSmallerDimension()
    {
        Circle sut = new() { Width = 50, Height = 200 };

        sut.Radius.ShouldBe(25f);
    }

    [Fact]
    public void Radius_WhenSquare_IsHalfDimension()
    {
        Circle sut = new() { Width = 80, Height = 80 };

        sut.Radius.ShouldBe(40f);
    }

    [Fact]
    public void Radius_Setter_KeepsWidthAndHeightSquare()
    {
        Circle sut = new();

        sut.Radius = 30;

        sut.Width.ShouldBe(60f);
        sut.Height.ShouldBe(60f);
    }

    // Issue #2851 — Apos.Shapes previously drew dropshadows at their own DropshadowColor
    // alpha while ignoring the body's alpha, so a shape fading to transparent left an opaque
    // shadow ghost behind. SkiaGum (the tool/viewport) multiplies shadow alpha by body alpha,
    // and the two backends must agree. EffectiveDropshadowColor is the multiplied value the
    // Render path now forwards to Apos.

    [Fact]
    public void EffectiveDropshadowColor_BodyFullyOpaque_PreservesShadowAlpha()
    {
        Circle sut = new()
        {
            Color = new Color(10, 20, 30, 255),
            DropshadowColor = new Color(100, 110, 120, 200),
        };

        sut.EffectiveDropshadowColor.ShouldBe(new Color(100, 110, 120, 200));
    }

    [Fact]
    public void EffectiveDropshadowColor_BodyHalfTransparent_HalvesShadowAlpha()
    {
        Circle sut = new()
        {
            Color = new Color(10, 20, 30, 128),
            DropshadowColor = new Color(100, 110, 120, 200),
        };

        // 200 * 128 / 255 = 100 (integer truncation matches the Render path).
        sut.EffectiveDropshadowColor.ShouldBe(new Color(100, 110, 120, 100));
    }

    [Fact]
    public void EffectiveDropshadowColor_BodyFullyTransparent_ZeroesShadowAlpha()
    {
        Circle sut = new()
        {
            Color = new Color(10, 20, 30, 0),
            DropshadowColor = new Color(100, 110, 120, 255),
        };

        sut.EffectiveDropshadowColor.A.ShouldBe((byte)0);
    }

    [Fact]
    public void EffectiveDropshadowColor_PreservesShadowRgb()
    {
        Circle sut = new()
        {
            Color = new Color(10, 20, 30, 64),
            DropshadowColor = new Color(100, 110, 120, 200),
        };

        Color effective = sut.EffectiveDropshadowColor;
        effective.R.ShouldBe((byte)100);
        effective.G.ShouldBe((byte)110);
        effective.B.ShouldBe((byte)120);
    }

    // Issue #2950 — on a stroke-only Circle (no fill), when DropshadowBlur exceeds StrokeWidth
    // the Apos lineThickness arg (StrokeWidth - DropshadowBlur) goes <= 0 and the Apos shader
    // refuses to draw, so the shadow disappears entirely. Fix: clamp lineThickness to a small
    // positive epsilon so Apos still draws, and scale the shadow's starting alpha by
    // (StrokeWidth / DropshadowBlur) so the visible band represents the tail of the alpha
    // ramp instead of the (impossible) full ramp. Matches the user's expected behavior:
    // "start at a smaller alpha value and advance to 0."

    [Fact]
    public void ComputeStrokeShadowDrawParameters_FilledMode_LeavesValuesUnchanged()
    {
        Circle sut = new()
        {
            IsFilled = true,
            StrokeWidth = 2f,
            DropshadowBlurX = 8f,
        };

        Color baseColor = new(50, 60, 70, 200);
        (float strokeWidth, Color color) = sut.ComputeStrokeShadowDrawParameters(baseColor);

        strokeWidth.ShouldBe(-6f);
        color.ShouldBe(baseColor);
    }

    [Fact]
    public void ComputeStrokeShadowDrawParameters_StrokeOnly_BlurZero_LeavesValuesUnchanged()
    {
        Circle sut = new()
        {
            IsFilled = false,
            StrokeWidth = 3f,
            DropshadowBlurX = 0f,
        };

        Color baseColor = new(50, 60, 70, 200);
        (float strokeWidth, Color color) = sut.ComputeStrokeShadowDrawParameters(baseColor);

        strokeWidth.ShouldBe(3f);
        color.ShouldBe(baseColor);
    }

    [Fact]
    public void ComputeStrokeShadowDrawParameters_StrokeGreaterThanBlur_LeavesValuesUnchanged()
    {
        Circle sut = new()
        {
            IsFilled = false,
            StrokeWidth = 10f,
            DropshadowBlurX = 4f,
        };

        Color baseColor = new(50, 60, 70, 200);
        (float strokeWidth, Color color) = sut.ComputeStrokeShadowDrawParameters(baseColor);

        strokeWidth.ShouldBe(6f);
        color.ShouldBe(baseColor);
    }

    [Fact]
    public void ComputeStrokeShadowDrawParameters_StrokeEqualsBlur_FullAlpha_PositiveStrokeWidth()
    {
        // At stroke = blur, lineThickness = 0 — Apos would bail. Engage the fix with full alpha
        // (ratio = 1) and a small positive stroke width so Apos still draws the AA-only band.
        Circle sut = new()
        {
            IsFilled = false,
            StrokeWidth = 4f,
            DropshadowBlurX = 4f,
        };

        Color baseColor = new(50, 60, 70, 200);
        (float strokeWidth, Color color) = sut.ComputeStrokeShadowDrawParameters(baseColor);

        strokeWidth.ShouldBeGreaterThan(0f);
        color.A.ShouldBe((byte)200);
    }

    [Fact]
    public void ComputeStrokeShadowDrawParameters_StrokeHalfOfBlur_HalvesStartingAlpha()
    {
        Circle sut = new()
        {
            IsFilled = false,
            StrokeWidth = 2f,
            DropshadowBlurX = 4f,
        };

        Color baseColor = new(50, 60, 70, 200);
        (float strokeWidth, Color color) = sut.ComputeStrokeShadowDrawParameters(baseColor);

        strokeWidth.ShouldBeGreaterThan(0f);
        color.A.ShouldBe((byte)100);
        // RGB preserved — only alpha is scaled.
        color.R.ShouldBe((byte)50);
        color.G.ShouldBe((byte)60);
        color.B.ShouldBe((byte)70);
    }

    [Fact]
    public void ComputeStrokeShadowDrawParameters_ZeroStrokeWithBlur_ZeroAlpha()
    {
        // stroke = 0, blur > 0 → ratio = 0 → shadow effectively invisible.
        Circle sut = new()
        {
            IsFilled = false,
            StrokeWidth = 0f,
            DropshadowBlurX = 4f,
        };

        Color baseColor = new(50, 60, 70, 200);
        (float strokeWidth, Color color) = sut.ComputeStrokeShadowDrawParameters(baseColor);

        strokeWidth.ShouldBeGreaterThan(0f);
        color.A.ShouldBe((byte)0);
    }
}
