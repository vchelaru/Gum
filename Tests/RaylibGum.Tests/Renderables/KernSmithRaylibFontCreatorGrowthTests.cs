using System;
using System.IO;
using System.Linq;
using KernSmith.Gum;
using RaylibGum.Helpers;
using RaylibGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Issue #4546 -- <see cref="KernSmithRaylibFontCreator.TryAddGlyphs(ref Raylib_cs.Font, BmfcSave, string)"/>
/// grows a font this creator already built via <see cref="KernSmithRaylibFontCreator.TryCreateFont"/>.
/// Uses a real GPU/GL context (RaylibGum.Tests runs headless via llvmpipe in CI) and a real font file,
/// mirroring <see cref="KernSmithRaylibFontCreatorTests"/> and the MonoGame-side growth tests --
/// hand-crafting a fake Raylib_cs.Font (native Recs/Glyphs pointer arrays) is impractical from managed
/// test code.
/// </summary>
public class KernSmithRaylibFontCreatorGrowthTests : BaseTestClass
{
    private static string FixtureFontPath =>
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");

    private static BmfcSave BuildBmfcSave(string ranges, float fontSize = 24) => new BmfcSave
    {
        FontName = "Orbitron-Black",
        FontFile = FixtureFontPath,
        FontSize = fontSize,
        UseSmoothing = true,
        Ranges = ranges,
    };

    [Fact]
    public void TryAddGlyphs_AddsANewCharacter_GrowsGlyphCountAndBlitsPixels()
    {
        KernSmithRaylibFontCreator creator = new();
        IGrowableRaylibFontCreator growable = creator;

        BmfcSave bmfcSave = BuildBmfcSave(ranges: "65"); // just 'A'
        Raylib_cs.Font? created = creator.TryCreateFont(bmfcSave);
        created.ShouldNotBeNull();
        Raylib_cs.Font font = created!.Value;
        int originalGlyphCount = font.GlyphCount;
        font.HasCharacter('B').ShouldBeFalse();

        var failed = growable.TryAddGlyphs(ref font, bmfcSave, "B");

        failed.ShouldNotBeNull();
        failed.ShouldBeEmpty();
        font.GlyphCount.ShouldBe(originalGlyphCount + 1);
        font.HasCharacter('B').ShouldBeTrue();

        Raylib_cs.Rectangle bRect = font.TryGetGlyphRectangle('B')!.Value;
        (bRect.Width * bRect.Height).ShouldBeGreaterThan(0,
            "because 'B' must have real, non-fallback glyph dimensions after growth");

        Raylib_cs.Image image = Raylib_cs.Raylib.LoadImageFromTexture(font.Texture);
        try
        {
            bool anyOpaquePixelInGlyphRegion = false;
            unsafe
            {
                byte* pixels = (byte*)image.Data;
                for (int y = (int)bRect.Y; y < bRect.Y + bRect.Height && !anyOpaquePixelInGlyphRegion; y++)
                {
                    for (int x = (int)bRect.X; x < bRect.X + bRect.Width; x++)
                    {
                        byte alpha = pixels[(y * image.Width + x) * 4 + 3];
                        if (alpha > 0)
                        {
                            anyOpaquePixelInGlyphRegion = true;
                            break;
                        }
                    }
                }
            }
            anyOpaquePixelInGlyphRegion.ShouldBeTrue("because the new glyph's pixel bytes must actually be blitted into the live texture");
        }
        finally
        {
            Raylib_cs.Raylib.UnloadImage(image);
        }
    }

    [Fact]
    public void TryAddGlyphs_WhenFontWasNotCreatedByThisCreator_ReturnsNull()
    {
        KernSmithRaylibFontCreator creator = new();
        IGrowableRaylibFontCreator growable = creator;

        Raylib_cs.Font untrackedFont = Raylib_cs.Raylib.GetFontDefault();

        var result = growable.TryAddGlyphs(ref untrackedFont, BuildBmfcSave(ranges: "65"), "B");

        result.ShouldBeNull();
    }

    [Fact]
    public void TryAddGlyphs_WhenACharacterHasNoGlyphInTheFontFile_ListsItAsFailed()
    {
        KernSmithRaylibFontCreator creator = new();
        IGrowableRaylibFontCreator growable = creator;

        // Same fixture/codepoint choice as the MonoGame-side growth tests: U+2192 ('->') is confirmed
        // absent from Orbitron-Black.ttf's cmap.
        BmfcSave bmfcSave = BuildBmfcSave(ranges: "65");
        Raylib_cs.Font? created = creator.TryCreateFont(bmfcSave);
        created.ShouldNotBeNull();
        Raylib_cs.Font font = created!.Value;

        var failed = growable.TryAddGlyphs(ref font, bmfcSave, "B→");

        failed.ShouldNotBeNull();
        failed.ShouldBe(new[] { '→' });
        font.HasCharacter('B').ShouldBeTrue("because a failure for one requested character must not block the others in the same batch");
        font.HasCharacter('→').ShouldBeFalse();
    }

    [Fact]
    public void TryAddGlyphs_WhenAtlasGrows_ReallocatesTextureAndKeepsExistingGlyphPixelPositions()
    {
        KernSmithRaylibFontCreator creator = new();
        IGrowableRaylibFontCreator growable = creator;

        // A generous font size against a tiny max atlas ceiling forces a Grow on the second add.
        BmfcSave bmfcSave = BuildBmfcSave(ranges: "65", fontSize: 48);
        bmfcSave.OutputWidth = 64;
        bmfcSave.OutputHeight = 64;
        Raylib_cs.Font? created = creator.TryCreateFont(bmfcSave);
        created.ShouldNotBeNull();
        Raylib_cs.Font font = created!.Value;

        Raylib_cs.Rectangle originalARect = font.TryGetGlyphRectangle('A')!.Value;
        int originalTextureWidth = font.Texture.Width;

        bmfcSave.OutputWidth = 4096;
        bmfcSave.OutputHeight = 4096;
        var failed = growable.TryAddGlyphs(ref font, bmfcSave, "WWWWWWWWWWWWWWWW");

        failed.ShouldNotBeNull();

        Raylib_cs.Rectangle rescaledARect = font.TryGetGlyphRectangle('A')!.Value;
        rescaledARect.X.ShouldBe(originalARect.X, "because a grow must never move an already-placed glyph's pixel position");
        rescaledARect.Y.ShouldBe(originalARect.Y);

        font.Texture.Width.ShouldBeGreaterThan(originalTextureWidth, "because the atlas must actually have grown to fit the new batch");
    }

    // Issue #4546 review finding: growth must not fork (rebuild the texture + Recs/Glyphs arrays) on
    // every call. A generous atlas ceiling means the first growth's own atlas resize already reserves
    // headroom well beyond what one extra character needs, so a second, unrelated character added
    // afterward must land in that same reserved room -- patched into the STILL-LIVE texture rather
    // than forcing another full rebuild.
    [Fact]
    public void TryAddGlyphs_SecondUnrelatedCharacter_ReusesTheSameTextureInsteadOfRebuilding()
    {
        KernSmithRaylibFontCreator creator = new();
        IGrowableRaylibFontCreator growable = creator;

        BmfcSave bmfcSave = BuildBmfcSave(ranges: "65"); // just 'A'
        Raylib_cs.Font font = creator.TryCreateFont(bmfcSave)!.Value;

        growable.TryAddGlyphs(ref font, bmfcSave, "B").ShouldNotBeNull();
        uint textureIdAfterFirstGrowth = font.Texture.Id;
        int glyphCountAfterFirstGrowth = font.GlyphCount;

        growable.TryAddGlyphs(ref font, bmfcSave, "C").ShouldNotBeNull();

        font.Texture.Id.ShouldBe(textureIdAfterFirstGrowth,
            "because the second character must be patched into the atlas the first growth already reserved, not force a whole new texture");
        font.GlyphCount.ShouldBe(glyphCountAfterFirstGrowth + 1);
        font.HasCharacter('C').ShouldBeTrue();
    }

    [Fact]
    public void TryAddGlyphs_TwoStructCopiesOfTheSameFont_BothConvergeOnTheSameCanonicalGeneration()
    {
        // Issue #4546: Raylib_cs.Font is a value type, so two Texts that resolved the same font
        // identity (e.g. both hit the same LoaderManager cache entry, mirroring the real runtime path
        // in CustomSetPropertyOnRenderable) each hold their OWN struct copy. Growing through one
        // Text's copy must not strand the other -- once IT ALSO calls TryAddGlyphs (because it, too,
        // is missing some character), it must land on the SAME canonical generation, not a second,
        // independent one.
        KernSmithRaylibFontCreator creator = new();
        IGrowableRaylibFontCreator growable = creator;

        BmfcSave bmfcSave = BuildBmfcSave(ranges: "65"); // just 'A'
        Raylib_cs.Font created = creator.TryCreateFont(bmfcSave)!.Value;
        Raylib_cs.Font firstCopy = created;
        Raylib_cs.Font secondCopy = created; // both Texts resolved the SAME cached value

        growable.TryAddGlyphs(ref firstCopy, bmfcSave, "B").ShouldNotBeNull();
        var result = growable.TryAddGlyphs(ref secondCopy, bmfcSave, "B");

        result.ShouldNotBeNull();
        secondCopy.Texture.Id.ShouldBe(firstCopy.Texture.Id);
        secondCopy.GlyphCount.ShouldBe(firstCopy.GlyphCount);
    }
}
