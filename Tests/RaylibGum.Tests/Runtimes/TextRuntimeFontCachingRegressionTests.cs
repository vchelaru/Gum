using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

// Regression coverage for the raylib font path rewritten in f5ad44e2b (#3173, "Raylib font parity via
// KernSmith"). That commit added a cache-hit early-return to UpdateToFontValues. EVERY pre-existing
// raylib font test sets LoaderManager.Self.CacheTextures = false, which makes that branch dead — so it
// shipped untested. With caching ON (the real-world default, e.g. the Samples/raylib gallery) and no
// FontCache .fnt files present, the branch hands every text after the first an empty (BaseSize 0) font.
public class TextRuntimeFontCachingRegressionTests : BaseTestClass
{
    public TextRuntimeFontCachingRegressionTests()
    {
        // Mirrors TextRuntimeTests: the renderable text measure throws if no window is ready.
        if (!Raylib.IsWindowReady())
        {
            Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
            Raylib.InitWindow(800, 600, "Test Window");
        }
    }

    // THE BUG (Samples/raylib gallery, first load): caching on, no FontCache .fnt on disk. The first
    // text's failed .fnt load caches an empty (BaseSize 0) font under the cache key; the cache-hit
    // early-return then returns that empty font to every later text instead of falling through to the
    // FontFamily (system Arial) fallback the first text used. So later texts lose their font and, being
    // RelativeToChildren, collapse to zero height — which is the "stacking is broken" symptom.
    // Depends on system Arial (C:\Windows\Fonts\arial.ttf) for the fallback, exactly as the gallery does.
    [Fact]
    public void MultipleTexts_WhenFontCacheFileMissing_WithCachingOn_ShouldEachResolveAUsableFont()
    {
        WithCachingOnAndNoFontFiles(() =>
        {
            TextRuntime first = NewArial18Text("List item 1");
            TextRuntime second = NewArial18Text("List item 2");
            TextRuntime third = NewArial18Text("List item 3");

            foreach (TextRuntime textRuntime in new[] { first, second, third })
            {
                Gum.Renderables.Text renderable = (Gum.Renderables.Text)textRuntime.RenderableComponent;
                renderable.Font.BaseSize.ShouldBeGreaterThan(0,
                    "every text must resolve a usable fallback font, not the empty (BaseSize 0) font cached after the .fnt load failed");
                textRuntime.GetAbsoluteHeight().ShouldBeGreaterThan(0,
                    "a text with a usable font and Height=RelativeToChildren must measure to a non-zero line height");
            }
        });
    }

    // Positive guard: a second text sharing an ALREADY-LOADED valid font must still take the cache-hit
    // branch and measure to the font's full .fnt line height (21 for Font18Arial). Keeps the VRAM-reuse
    // optimization the cache-hit was added for from being broken by the fix above.
    [Fact]
    public void SecondText_SharingValidCachedFont_ShouldMeasureToFntLineHeight()
    {
        WithFont18ArialCached(() =>
        {
            TextRuntime first = NewArial18Text("List item 1");
            first.GetAbsoluteHeight().ShouldBe(21);

            TextRuntime second = NewArial18Text("List item 2");
            second.GetAbsoluteHeight().ShouldBe(21);
        });
    }

    // Positive guard for the user-visible symptom: with valid fonts, a vertical stack must reflow to the
    // summed line heights (2 x 21) and the second item must sit below the first.
    [Fact]
    public void VerticalStack_OfValidCachedFontTexts_ShouldReflowToSummedLineHeights()
    {
        WithFont18ArialCached(() =>
        {
            ContainerRuntime stack = new();
            stack.WidthUnits = DimensionUnitType.RelativeToChildren;
            stack.HeightUnits = DimensionUnitType.RelativeToChildren;
            stack.Width = 0;
            stack.Height = 0;
            stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;

            TextRuntime first = NewArial18Text("List item 1");
            TextRuntime second = NewArial18Text("List item 2");
            stack.AddChild(first);
            stack.AddChild(second);

            stack.UpdateLayout();

            first.GetAbsoluteHeight().ShouldBe(21);
            second.GetAbsoluteHeight().ShouldBe(21);
            stack.GetAbsoluteHeight().ShouldBe(42);
            second.AbsoluteTop.ShouldBe(first.AbsoluteTop + 21);
        });
    }

    private static TextRuntime NewArial18Text(string text)
    {
        TextRuntime textRuntime = new();
        textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
        textRuntime.Width = 0;
        textRuntime.HeightUnits = DimensionUnitType.RelativeToChildren;
        textRuntime.Height = 0;
        textRuntime.SetProperty("Font", "Arial");
        textRuntime.SetProperty("FontSize", 18);
        textRuntime.Text = text;
        return textRuntime;
    }

    // Caching ON (real-world default) with a unique relative directory and NO stream hook: the
    // (Arial, 18) FontCache .fnt cannot be resolved from disk or hook, so the load fails — reproducing
    // the Samples/raylib gallery, which ships no FontCache files and relies on the FontFamily fallback.
    private static void WithCachingOnAndNoFontFiles(Action body)
    {
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        string savedRelativeDirectory = FileManager.RelativeDirectory;
        Func<string, Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        try
        {
            LoaderManager.Self.CacheTextures = true;
            FileManager.RelativeDirectory = Path.Combine(Path.GetTempPath(),
                "GumRaylibMissingFnt_" + Guid.NewGuid().ToString("N")).Replace('\\', '/') + "/";
            FileManager.CustomGetStreamFromFile = null;

            body();
        }
        finally
        {
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.RelativeDirectory = savedRelativeDirectory;
            FileManager.CustomGetStreamFromFile = savedHook;
        }
    }

    // Serves the gold Font18Arial fixture (.fnt + page) in memory with caching ON, so the (Arial, 18)
    // cache file resolves and a VALID font is cached. A unique relative directory keeps the cache key
    // unique to this run so a prior test can't mask the result.
    private static void WithFont18ArialCached(Action body)
    {
        string fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "FontCache");
        Dictionary<string, byte[]> inMemoryFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Font18Arial.fnt", File.ReadAllBytes(Path.Combine(fixtureDirectory, "Font18Arial.fnt")) },
            { "Font18Arial_0.png", File.ReadAllBytes(Path.Combine(fixtureDirectory, "Font18Arial_0.png")) },
        };

        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        string savedRelativeDirectory = FileManager.RelativeDirectory;
        Func<string, Stream>? savedHook = FileManager.CustomGetStreamFromFile;
        try
        {
            LoaderManager.Self.CacheTextures = true;
            FileManager.RelativeDirectory = Path.Combine(Path.GetTempPath(),
                "GumRaylibFontCacheTest_" + Guid.NewGuid().ToString("N")).Replace('\\', '/') + "/";
            FileManager.CustomGetStreamFromFile = incomingPath =>
                inMemoryFiles.TryGetValue(Path.GetFileName(incomingPath), out byte[]? bytes)
                    ? new MemoryStream(bytes)
                    : null!;

            body();
        }
        finally
        {
            LoaderManager.Self.CacheTextures = savedCacheTextures;
            FileManager.RelativeDirectory = savedRelativeDirectory;
            FileManager.CustomGetStreamFromFile = savedHook;
        }
    }
}
