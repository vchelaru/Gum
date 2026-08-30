using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// #4464: a wired IInMemoryFontCreator (e.g. KernSmith) that throws or declines was silently
// swallowed on MonoGame/KNI/FNA, falling back to the default font with zero diagnostics anywhere.
// Raylib's equivalent catch sites were fixed first (#4465); this pins the matching XNALIKE fix in
// CustomSetPropertyOnRenderable's two TryCreateFont catch sites (the main GetOrCreateBakedFont path
// and the inline BBCode-run GetAndCreateFontIfNecessary path) plus GetOrCreateBakedFont's new
// "declined and nothing else resolved either" notification.
//
// Every font name used here is GUID-suffixed and never pulls in the shared Arial-18 embedded-resource
// stub (LoaderManager.Self is a process-wide static other test files also populate/query): a fixed
// name could collide with another test's cached entry and short-circuit resolution before it ever
// reaches the creator under test.
public class InMemoryFontCreatorPropertyAssignmentErrorTests : BaseTestClass
{
    [Fact]
    public void GetOrCreateBakedFont_WhenInMemoryFontCreatorThrows_ShouldInvokePropertyAssignmentError()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        string? capturedMessage = null;
        void Handler(string message) => capturedMessage = message;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new ThrowingFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = UniqueFontName();
            textRuntime.FontSize = 12;

            capturedMessage.ShouldNotBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }
    }

    [Fact]
    public void GetAndCreateFontIfNecessary_WhenInMemoryFontCreatorThrows_ShouldInvokePropertyAssignmentError()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        string? capturedMessage = null;
        void Handler(string message) => capturedMessage = message;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            string baseFontName = UniqueFontName();
            string inlineFontName = UniqueFontName();
            // Pre-caching the base font's exact resolution key means the base font never consults the
            // creator at all, isolating the failure below to the inline [Font=...] run.
            LoaderManager.Self.AddDisposable(
                GetPlainFontCacheKey(baseFontName, fontSize: 20), NewStubFont());

            CustomSetPropertyOnRenderable.InMemoryFontCreator = new ThrowingFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = baseFontName;
            // The Font assignment above already triggered a resolution at the TextRuntime's default
            // FontSize (not yet 20), which the creator throws on -- reset before the FontSize
            // assignment below, which is the one under test (baseFontName, 20 -> the pre-cached hit).
            capturedMessage = null;
            textRuntime.FontSize = 20;
            capturedMessage.ShouldBeNull(
                "because the base font resolves from the pre-cached entry without consulting the creator");

            textRuntime.Text = $"AA [Font={inlineFontName}]BB[/Font] CC";

            capturedMessage.ShouldNotBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }
    }

    [Fact]
    public void GetOrCreateBakedFont_WhenCreatorDeclinesAndNoFallbackResolves_ShouldInvokePropertyAssignmentError()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        string? capturedMessage = null;
        void Handler(string message) => capturedMessage = message;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            DecliningFontCreator creator = new();
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new();
            // A GUID-unique name has no FontCache .fnt on disk and no cached/embedded entry, so
            // resolution cannot be satisfied by anything other than the (declining) creator.
            textRuntime.Font = UniqueFontName();
            textRuntime.FontSize = 12;

            creator.CallCount.ShouldBeGreaterThan(0);
            capturedMessage.ShouldNotBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }
    }

    // The guard the fix above must not break: a creator wired but never even consulted (because the
    // font was already resolved from cache) must stay completely silent -- otherwise every project
    // with a wired creator would get a spurious error on every successfully-resolved font.
    [Fact]
    public void GetOrCreateBakedFont_WhenFontResolvesFromCache_ShouldNotInvokePropertyAssignmentError()
    {
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        string? capturedMessage = null;
        void Handler(string message) => capturedMessage = message;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            string fontName = UniqueFontName();
            LoaderManager.Self.AddDisposable(
                GetPlainFontCacheKey(fontName, fontSize: 14), NewStubFont());

            // Throws if ever consulted, so this test fails loudly (not silently-vacuously) if the
            // cache-hit short-circuit stops gating the creator out.
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new ThrowingFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = fontName;
            // The Font assignment above already triggered a resolution at the TextRuntime's default
            // FontSize (not yet 14), which the creator throws on -- reset before the FontSize
            // assignment below, which is the one under test (fontName, 14 -> the pre-cached hit).
            capturedMessage = null;
            textRuntime.FontSize = 14;

            capturedMessage.ShouldBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }
    }

    private static string UniqueFontName() => "GumFontCreatorErrorTest_" + Guid.NewGuid().ToString("N");

    // Mirrors GetOrCreateBakedFont's own key computation (Gum/Wireframe/CustomSetPropertyOnRenderable.cs):
    // fullFileName = FileManager.Standardize(textRuntime.GetFontCacheFileName(fontFilePath), preserveCase:
    // true, makeAbsolute: true), for a plain family-name/size font (no CustomFontFile, no italic/bold/
    // dropshadow -- all TextRuntime defaults).
    private static string GetPlainFontCacheKey(string fontName, float fontSize) =>
        FileManager.Standardize(
            BmfcSave.GetFontCacheFileNameFor(fontSize, fontName, outline: 0, useFontSmoothing: true),
            preserveCase: true, makeAbsolute: true);

    // A minimal, valid, standalone BitmapFont (space + A/B/C glyphs, no disk I/O, no texture needed).
    private static BitmapFont NewStubFont()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, StubFontData);
        font.SetFontPattern(256, 256);
        return font;
    }

    private const string StubFontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=18 base=18 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=4
char id=32 x=0 y=0 width=9 height=13 xoffset=0 yoffset=4 xadvance=9 page=0 chnl=15
char id=65 x=0 y=0 width=9 height=13 xoffset=0 yoffset=4 xadvance=9 page=0 chnl=15
char id=66 x=0 y=0 width=9 height=13 xoffset=0 yoffset=4 xadvance=9 page=0 chnl=15
char id=67 x=0 y=0 width=9 height=13 xoffset=0 yoffset=4 xadvance=9 page=0 chnl=15
";

    // Always throws, simulating a rasterizer failure (e.g. KernSmith failing under a WASM host) so the
    // catch blocks around InMemoryFontCreator.TryCreateFont are exercised.
    private sealed class ThrowingFontCreator : IInMemoryFontCreator
    {
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
            => throw new InvalidOperationException("Simulated font rasterization failure.");
    }

    // Always declines (returns null) -- IInMemoryFontCreator.TryCreateFont's own documented contract
    // for "creation fails or is not supported," not an exception.
    private sealed class DecliningFontCreator : IInMemoryFontCreator
    {
        public int CallCount { get; private set; }

        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            CallCount++;
            return null;
        }
    }
}
