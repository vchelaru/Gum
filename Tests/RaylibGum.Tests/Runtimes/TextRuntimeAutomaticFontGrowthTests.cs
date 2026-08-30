using System;
using System.Collections.Generic;
using System.IO;
using Gum.GueDeriving;
using Gum.Renderables;
using KernSmith.Gum;
using RaylibGum.Helpers;
using RaylibGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

// Issue #4546 -- Raylib parity for #4542 (TextRuntime.UseAutomaticFontGrowth). Uses the real
// KernSmithRaylibFontCreator (wrapped by a recording spy, not a synthetic fake), same rationale as
// TextRuntimeFontOversamplingTests: hand-crafting a fake Raylib_cs.Font (native Recs/Glyphs pointer
// arrays) is impractical from managed test code. Growth requires a real font FILE (KernSmith's
// incremental sessions have no system-font overload), so these assign Font directly to a .ttf path
// (BmfcSave.ResolveTtfSourcePath's "Font-as-path" branch) rather than a family name like "Arial".
public class TextRuntimeAutomaticFontGrowthTests : BaseTestClass
{
    private static string FixtureFontPath =>
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");

    [Fact]
    public void Text_WhenAutomaticFontGrowthDisabled_MissingCharacterStaysUngrown()
    {
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = false;
            SpyGrowableRaylibFontCreator creator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.Font = FixtureFontPath;
            // U+20AC ('€') is confirmed present in Orbitron-Black.ttf's cmap but outside the default
            // Ranges (32-126,160-255), so it's only reachable via growth -- with growth disabled it
            // must stay ungrown.
            textRuntime.Text = "€";

            Text text = (Text)textRuntime.RenderableComponent;
            text.Font.HasCharacter('€').ShouldBeFalse();
            creator.TryAddGlyphsCalls.ShouldBeEmpty();
        }
        finally
        {
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_WhenAutomaticFontGrowthEnabled_MissingCharacterIsGrownSynchronously()
    {
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = true;
            SpyGrowableRaylibFontCreator creator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.Font = FixtureFontPath;
            textRuntime.Text = "€"; // present in the font but outside the default Ranges -- see test above

            Text text = (Text)textRuntime.RenderableComponent;
            text.Font.HasCharacter('€').ShouldBeTrue(
                "because the missing character must be grown synchronously, in the same Text assignment that discovered it");
            creator.TryAddGlyphsCalls.ShouldContain("€");
        }
        finally
        {
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_WhenAutomaticFontGrowthEnabled_AllCharactersAlreadyPresent_NeverCallsGrowth()
    {
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = true;
            SpyGrowableRaylibFontCreator creator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.Font = FixtureFontPath;
            textRuntime.Text = "€"; // grows it once
            creator.TryAddGlyphsCalls.Clear();

            textRuntime.Text = "€€"; // same character again -- already grown, nothing left missing

            creator.TryAddGlyphsCalls.ShouldBeEmpty(
                "because a font that already has every requested character must never be asked to grow");
        }
        finally
        {
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_WhenACharacterCannotBeRendered_RaisesPropertyAssignmentError_AndDoesNotGrowIt()
    {
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        List<string> reportedMessages = new();
        Action<string> handler = reportedMessages.Add;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = true;
            SpyGrowableRaylibFontCreator creator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
            CustomSetPropertyOnRenderable.PropertyAssignmentError += handler;

            TextRuntime textRuntime = new();
            textRuntime.Font = FixtureFontPath;
            reportedMessages.Clear(); // isolate from whatever the Font/constructor cascade already reported

            // U+2192 ('->') is confirmed absent from Orbitron-Black.ttf's cmap.
            textRuntime.Text = "→";

            Text text = (Text)textRuntime.RenderableComponent;
            text.Font.HasCharacter('→').ShouldBeFalse();
            reportedMessages.ShouldHaveSingleItem();
            reportedMessages[0].ShouldContain("→");
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= handler;
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // BmfcSave.OutputWidth/OutputHeight default to 512x256 -- sized for a small, disk-persisted
    // .fnt/.png cache file. Reused verbatim as a growth ceiling, that fills after only a handful of
    // glyphs at any real FontSize and throws a KernSmith atlas-packing exception. Growth must raise
    // the ceiling to TextRuntime.MaxInMemoryFontAtlasSize.
    [Fact]
    public void Text_WhenGrowing_UsesMaxInMemoryFontAtlasSize_NotBmfcSavesSmallDiskCacheDefault()
    {
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = true;
            SpyGrowableRaylibFontCreator creator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.Font = FixtureFontPath;
            textRuntime.Text = "€";

            creator.LastBmfcSave.ShouldNotBeNull();
            creator.LastBmfcSave!.OutputWidth.ShouldBe(TextRuntime.MaxInMemoryFontAtlasSize);
            creator.LastBmfcSave.OutputHeight.ShouldBe(TextRuntime.MaxInMemoryFontAtlasSize);
        }
        finally
        {
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_WhenInMemoryFontCreatorDoesNotSupportGrowth_DoesNothingSilently()
    {
        bool savedFlag = TextRuntime.UseAutomaticFontGrowth;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = true;
            // Implements IRaylibFontCreator only -- no IGrowableRaylibFontCreator, same as any
            // existing custom creator written before this feature existed.
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new NonGrowableRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = FixtureFontPath;

            Should.NotThrow(() => textRuntime.Text = "€");
        }
        finally
        {
            TextRuntime.UseAutomaticFontGrowth = savedFlag;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #4542 design decision #3 (Raylib parity): oversampling keeps two live fonts (pinned
    // MeasurementFont, regenerated display font). A full RegenerateOversampledFont builds a brand-new
    // font from BmfcSave.Ranges alone -- it has no idea about characters grown in at runtime, so
    // continuous zooming would silently drop them on every regenerate unless growth history is
    // replayed into each freshly-generated font.
    [Fact]
    public void RegenerateOversampledFont_ReplaysPreviouslyGrownCharacters_IntoTheFreshOversampledFont()
    {
        bool savedOversampling = TextRuntime.UseFontOversampling;
        bool savedGrowth = TextRuntime.UseAutomaticFontGrowth;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            TextRuntime.UseFontOversampling = true;
            TextRuntime.UseAutomaticFontGrowth = true;
            SpyGrowableRaylibFontCreator creator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            textRuntime.Font = FixtureFontPath;
            textRuntime.FontSize = 20;
            // U+2026 ('…') is confirmed present in Orbitron-Black.ttf's cmap but outside the default
            // Ranges, so assigning it forces a real growth event on the native font.
            textRuntime.Text = "…";

            Text text = (Text)textRuntime.RenderableComponent;
            text.Font.HasCharacter('…').ShouldBeTrue("because the native font must have grown first");

            bool result = textRuntime.RegenerateOversampledFont(2.5f);

            result.ShouldBeTrue();
            text.Font.HasCharacter('…').ShouldBeTrue(
                "because previously-grown characters must be replayed into every freshly-regenerated oversampled font");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedOversampling;
            TextRuntime.UseAutomaticFontGrowth = savedGrowth;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Wraps the real KernSmithRaylibFontCreator (not a synthetic fake, per this file's own header
    // comment) so tests can observe/count TryAddGlyphs calls the way MonoGame's stub fakes do.
    private sealed class SpyGrowableRaylibFontCreator : IRaylibFontCreator, IGrowableRaylibFontCreator
    {
        private readonly KernSmithRaylibFontCreator _inner = new();

        public List<string> TryAddGlyphsCalls { get; } = new();
        public BmfcSave? LastBmfcSave { get; private set; }

        public Raylib_cs.Font? TryCreateFont(BmfcSave bmfcSave) => _inner.TryCreateFont(bmfcSave);

        public IReadOnlyList<char>? TryAddGlyphs(ref Raylib_cs.Font font, BmfcSave bmfcSave, string characters)
        {
            LastBmfcSave = bmfcSave;
            TryAddGlyphsCalls.Add(characters);
            return ((IGrowableRaylibFontCreator)_inner).TryAddGlyphs(ref font, bmfcSave, characters);
        }
    }

    private sealed class NonGrowableRaylibFontCreator : IRaylibFontCreator
    {
        private readonly KernSmithRaylibFontCreator _inner = new();

        public Raylib_cs.Font? TryCreateFont(BmfcSave bmfcSave) => _inner.TryCreateFont(bmfcSave);
    }
}
