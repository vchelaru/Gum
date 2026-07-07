using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
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
            FileManager.RelativeDirectory = Path.Combine(Path.GetTempPath(),
                "GumBbCodeBoldFont_" + Guid.NewGuid().ToString("N")).Replace('\\', '/') + "/";

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
            // fix this threw ArgumentException from AddDisposable's default ThrowException behavior.
            Should.NotThrow(() =>
                textRuntime.Text = "normal [IsBold=true]bold[/IsBold] normal");
        }
        finally
        {
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.RelativeDirectory = savedRelativeDirectory;
        }
    }

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
}
