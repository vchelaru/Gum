using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Regression coverage for issue #3530: assigning a TextRuntime.Text that contains a BBCode
// [IsBold=true] run crashed a shipping FlatRedBall game with
// "System.ArgumentException: An item with the same key has already been added. Key: ...\fontcache\font12garet_book_bold.fnt".
//
// The inline-BBCode font resolver (CustomSetPropertyOnRenderable.GetAndCreateFontIfNecessary)
// resolves the same bold font once per open tag by design and relies on
// LoaderManager.GetDisposable(...) as BitmapFont to dedup. That guard is not robust to a poisoned
// cache slot: when an earlier resolution cached a value the "as BitmapFont" cast cannot recover
// (e.g. a null/placeholder cached under the bold key because no .fnt existed on disk and the
// DefaultBitmapFont fallback was used), the next lookup sees an empty slot, falls through, and its
// AddDisposable(...) call — which defaulted to ExistingContentBehavior.ThrowException — collides with
// the still-occupied key and throws. The fix passes ExistingContentBehavior.Replace so the poisoned
// slot heals instead of crashing.
//
// The resolver has TWO AddDisposable calls that were changed to Replace, reached by different font
// sources, so each has its own test:
//   * disk / DefaultBitmapFont fallback (no InMemoryFontCreator) -> the crash's exact line.
//   * in-memory font creation (InMemoryFontCreator set)          -> the sibling line, covered so a
//     regression that heals only one of the two lines still turns a test red.
// Each test also asserts the poisoned slot was actually REPLACED with a BitmapFont afterward. Without
// that, the test could pass vacuously if the key the resolver computes ever drifted away from the key
// this test poisons: the resolver would then add under a fresh (empty) key, never collide, and
// Should.NotThrow would be satisfied without the fix under test running at all.
public class TextRuntimeBbCodeFontCachingRegressionTests : BaseTestClass
{
    // Uses a font NOT in the test harness's stubbed embedded resources (which only cover Arial-18)
    // and a size (12) matching the real crash's Font12garet_book_bold key, so the resolution actually
    // reaches the disk / DefaultBitmapFont fallback path instead of being satisfied by an embedded font.
    private const string UnstubbedFontName = "Garet";
    private const int UnstubbedFontSize = 12;

    [Fact]
    public void Text_WithBbCodeBoldRun_WhenBoldCacheSlotPoisoned_ShouldNotThrowDuplicateKey()
    {
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        string savedRelativeDirectory = FileManager.RelativeDirectory;
        try
        {
            LoaderManager.Self.CacheTextures = true;
            // A unique relative directory makes the (absolute) font-cache key unique to this run so no
            // prior test can mask the collision and this test cannot poison a shared slot.
            FileManager.RelativeDirectory = MakeUniqueRelativeDirectory();

            // Reproduce the poisoned slot the real first resolution left behind: a cached value the
            // "as BitmapFont" cast cannot recover (so the dedup guard sees an empty slot and falls
            // through to AddDisposable) under the exact bold key the inline-BBCode resolver computes.
            string boldKey = GetBoldFontCacheKey();
            LoaderManager.Self.AddDisposable(boldKey, new NonFontDisposable(),
                LoaderManager.ExistingContentBehavior.Replace);

            TextRuntime textRuntime = new();
            textRuntime.Font = UnstubbedFontName;
            textRuntime.FontSize = UnstubbedFontSize;

            // Assigning BBCode with a bold run re-resolves the (already poisoned) bold key. Before the
            // fix this threw ArgumentException from AddDisposable's default ThrowException behavior. With
            // no InMemoryFontCreator set, resolution falls to the disk / DefaultBitmapFont path (the
            // exact line in the #3530 crash stack).
            Should.NotThrow(() =>
                textRuntime.Text = "normal [IsBold=true]bold[/IsBold] normal");

            // The resolver reached the poisoned key and replaced the non-font placeholder with the
            // resolved font, proving the fixed AddDisposable actually ran (not skipped via key drift).
            LoaderManager.Self.GetDisposable(boldKey).ShouldBeAssignableTo<BitmapFont>();
        }
        finally
        {
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.RelativeDirectory = savedRelativeDirectory;
        }
    }

    [Fact]
    public void Text_WithBbCodeBoldRun_WhenBoldSlotPoisonedAndResolvedByInMemoryCreator_ShouldNotThrowDuplicateKey()
    {
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        string savedRelativeDirectory = FileManager.RelativeDirectory;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            LoaderManager.Self.CacheTextures = true;
            FileManager.RelativeDirectory = MakeUniqueRelativeDirectory();
            // With an in-memory creator, the bold run resolves to a freshly created font and caches it
            // via the resolver's OTHER AddDisposable call (the in-memory branch), which the fix also
            // changed to Replace. The disk-fallback test above never reaches this branch because the
            // harness leaves InMemoryFontCreator null.
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new AbcInMemoryFontCreator();

            string boldKey = GetBoldFontCacheKey();
            LoaderManager.Self.AddDisposable(boldKey, new NonFontDisposable(),
                LoaderManager.ExistingContentBehavior.Replace);

            TextRuntime textRuntime = new();
            textRuntime.Font = UnstubbedFontName;
            textRuntime.FontSize = UnstubbedFontSize;

            // Only glyphs the stub font defines (space + A/B/C) so measurement stays valid; only "BB" is
            // bold, so it re-resolves the poisoned bold key.
            Should.NotThrow(() =>
                textRuntime.Text = "AA [IsBold=true]BB[/IsBold] CC");

            LoaderManager.Self.GetDisposable(boldKey).ShouldBeAssignableTo<BitmapFont>();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.RelativeDirectory = savedRelativeDirectory;
        }
    }

    // A unique relative directory makes the (absolute) font-cache key unique to this run so no prior
    // test can mask the collision and this test cannot poison a shared slot.
    private static string MakeUniqueRelativeDirectory() =>
        Path.Combine(Path.GetTempPath(), "GumBbCodeBoldFont_" + Guid.NewGuid().ToString("N"))
            .Replace('\\', '/') + "/";

    // Mirrors CustomSetPropertyOnRenderable.GetFontFileName: the same GetFontCacheFileNameFor call
    // followed by the same RemoveDotDotSlash(Standardize(name, false, true)) normalization.
    private static string GetBoldFontCacheKey()
    {
        string cacheFileName = BmfcSave.GetFontCacheFileNameFor(
            UnstubbedFontSize, UnstubbedFontName, outline: 0, useFontSmoothing: true,
            isItalic: false, isBold: true, fontFilePath: null);
        return FileManager.RemoveDotDotSlash(
            FileManager.Standardize(cacheFileName, preserveCase: false, makeAbsolute: true));
    }

    // A cached IDisposable that is not a BitmapFont, so the resolver's "GetDisposable(...) as
    // BitmapFont" guard yields null and treats the (occupied) slot as empty. Safe to Dispose when the
    // shared cache is later cleared, unlike a null cache value.
    private sealed class NonFontDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    // Minimal in-memory font creator: returns a valid BitmapFont (space + A/B/C glyphs, no disk I/O)
    // for any request, so the resolver takes the in-memory AddDisposable branch instead of the disk /
    // DefaultBitmapFont fallback.
    private sealed class AbcInMemoryFontCreator : IInMemoryFontCreator
    {
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            BitmapFont font = new BitmapFont((Texture2D)null!, AbcFontData);
            font.SetFontPattern(256, 256);
            return font;
        }

        private const string AbcFontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=18 base=18 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=4
char id=32 x=0 y=0 width=9 height=13 xoffset=0 yoffset=4 xadvance=9 page=0 chnl=15
char id=65 x=0 y=0 width=9 height=13 xoffset=0 yoffset=4 xadvance=9 page=0 chnl=15
char id=66 x=0 y=0 width=9 height=13 xoffset=0 yoffset=4 xadvance=9 page=0 chnl=15
char id=67 x=0 y=0 width=9 height=13 xoffset=0 yoffset=4 xadvance=9 page=0 chnl=15
";
    }
}
