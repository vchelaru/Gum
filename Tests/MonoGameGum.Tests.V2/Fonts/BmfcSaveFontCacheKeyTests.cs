using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V2.Fonts;

/// <summary>
/// Issue #4001 — the drop shadow is drawn at runtime as a separate silhouette (offset + tinted
/// under the glyph), so shadow color and offset no longer affect the baked atlas and must NOT be
/// part of the font cache key; keeping them there forced needless atlas regeneration on every
/// tweak. Blur still shapes the baked silhouette, so it stays in the key.
/// </summary>
public class BmfcSaveFontCacheKeyTests
{
    [Fact]
    public void CacheName_WithDropshadow_IgnoresShadowColor()
    {
        string redOpaque = BmfcSave.GetFontCacheFileNameFor(24, "Arial", 0, true, hasDropshadow: true,
            dropshadowBlur: 2f, dropshadowRed: 255, dropshadowGreen: 0, dropshadowBlue: 0, dropshadowAlpha: 255);
        string greenHalf = BmfcSave.GetFontCacheFileNameFor(24, "Arial", 0, true, hasDropshadow: true,
            dropshadowBlur: 2f, dropshadowRed: 0, dropshadowGreen: 255, dropshadowBlue: 0, dropshadowAlpha: 128);

        redOpaque.ShouldBe(greenHalf);
    }

    [Fact]
    public void CacheName_WithDropshadow_IgnoresShadowOffset()
    {
        string offsetA = BmfcSave.GetFontCacheFileNameFor(24, "Arial", 0, true, hasDropshadow: true,
            dropshadowOffsetX: 2f, dropshadowOffsetY: 2f, dropshadowBlur: 3f);
        string offsetB = BmfcSave.GetFontCacheFileNameFor(24, "Arial", 0, true, hasDropshadow: true,
            dropshadowOffsetX: 9f, dropshadowOffsetY: -4f, dropshadowBlur: 3f);

        offsetA.ShouldBe(offsetB);
    }

    [Fact]
    public void CacheName_WithDropshadow_DistinguishesBlur()
    {
        string blur2 = BmfcSave.GetFontCacheFileNameFor(24, "Arial", 0, true, hasDropshadow: true, dropshadowBlur: 2f);
        string blur5 = BmfcSave.GetFontCacheFileNameFor(24, "Arial", 0, true, hasDropshadow: true, dropshadowBlur: 5f);

        blur2.ShouldNotBe(blur5);
    }
}
