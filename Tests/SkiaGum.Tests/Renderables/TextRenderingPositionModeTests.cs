using Shouldly;
using SkiaGum;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies Skia's pixel-snap knob for <see cref="Text"/> (issue #3708). Unlike XNALIKE/Raylib,
/// Skia rasterizes glyphs live via RichTextKit rather than blitting a pre-baked glyph-atlas
/// texture, so snapping isn't about avoiding texture-sampling shimmer -- it keeps the
/// anti-aliasing pattern consistent frame to frame and across sibling items in a list.
/// </summary>
public class TextRenderingPositionModeTests
{
    [Fact]
    public void TextRenderingPositionMode_DefaultsToSnapToPixel()
    {
        Text.TextRenderingPositionMode.ShouldBe(TextRenderingPositionMode.SnapToPixel);
    }

    [Fact]
    public void EffectiveTextRenderingPositionMode_ShouldFallBackToStatic_WhenNoOverride()
    {
        TextRenderingPositionMode saved = Text.TextRenderingPositionMode;
        try
        {
            Text.TextRenderingPositionMode = TextRenderingPositionMode.FreeFloating;
            Text sut = new();
            sut.OverrideTextRenderingPositionMode = null;

            sut.EffectiveTextRenderingPositionMode.ShouldBe(TextRenderingPositionMode.FreeFloating);
        }
        finally
        {
            Text.TextRenderingPositionMode = saved;
        }
    }

    [Fact]
    public void EffectiveTextRenderingPositionMode_ShouldPreferInstanceOverride_OverStatic()
    {
        TextRenderingPositionMode saved = Text.TextRenderingPositionMode;
        try
        {
            Text.TextRenderingPositionMode = TextRenderingPositionMode.SnapToPixel;
            Text sut = new();
            sut.OverrideTextRenderingPositionMode = TextRenderingPositionMode.FreeFloating;

            sut.EffectiveTextRenderingPositionMode.ShouldBe(TextRenderingPositionMode.FreeFloating);
        }
        finally
        {
            Text.TextRenderingPositionMode = saved;
        }
    }

    [Fact]
    public void GetSnappedOrigin_RoundsToNearestWholePixel_WhenSnapToPixel()
    {
        Text sut = new();
        sut.OverrideTextRenderingPositionMode = TextRenderingPositionMode.SnapToPixel;

        (float X, float Y) result = sut.GetSnappedOrigin(10.6f, 20.4f);

        result.X.ShouldBe(11f);
        result.Y.ShouldBe(20f);
    }

    [Fact]
    public void GetSnappedOrigin_ReturnsValueUnchanged_WhenFreeFloating()
    {
        Text sut = new();
        sut.OverrideTextRenderingPositionMode = TextRenderingPositionMode.FreeFloating;

        (float X, float Y) result = sut.GetSnappedOrigin(10.6f, 20.4f);

        result.X.ShouldBe(10.6f);
        result.Y.ShouldBe(20.4f);
    }
}
