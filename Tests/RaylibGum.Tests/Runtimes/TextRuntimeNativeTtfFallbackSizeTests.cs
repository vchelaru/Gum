using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// #6591: with no font creator, a TTF that raylib loads natively is rasterized at the Text's own
/// FontSize, and each size is cached and disposed on its own.
/// </summary>
public class TextRuntimeNativeTtfFallbackSizeTests : BaseTestClass
{
    private readonly IRaylibFontCreator? _previousCreator;
    private readonly bool _previousCacheTextures;
    private readonly List<string> _cacheKeys = new();

    public TextRuntimeNativeTtfFallbackSizeTests()
    {
        _previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
        _previousCacheTextures = LoaderManager.Self.CacheTextures;
        LoaderManager.Self.CacheTextures = true;
    }

    public override void Dispose()
    {
        foreach (string key in _cacheKeys)
        {
            LoaderManager.Self.Dispose(key);
        }
        LoaderManager.Self.CacheTextures = _previousCacheTextures;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = _previousCreator;
        base.Dispose();
    }

    [Fact]
    public void FontSize_TwoTextsSameFontFile_EachLoadsAtItsOwnSize()
    {
        string path = FontPath("OpenSans-Regular.ttf");

        TextRuntime small = CreateText(path, 12);
        TextRuntime large = CreateText(path, 48);

        FontOf(small).BaseSize.ShouldBe(12);
        FontOf(large).BaseSize.ShouldBe(48);
        FontOf(small).Texture.Id.ShouldNotBe(FontOf(large).Texture.Id);

        float smallCapHeight = CapHeight(FontOf(small));
        float largeCapHeight = CapHeight(FontOf(large));
        largeCapHeight.ShouldBe(smallCapHeight * 4, smallCapHeight * 0.5f,
            "the 48 font must be rasterized at 48, not scaled from a shared atlas");
    }

    [Fact]
    public void FontSize_SingleText_MatchesTtfFontLoaderAtThatSize()
    {
        string path = FontPath("OpenSans-Regular.ttf");
        Font expected = TtfFontLoader.LoadFont(path, 24);
        try
        {
            TextRuntime text = CreateText(path, 24);

            FontOf(text).BaseSize.ShouldBe(24);
            CapHeight(FontOf(text)).ShouldBe(CapHeight(expected));
            ((float)((Text)text.RenderableComponent!).LineHeightInPixels).ShouldBe(LineHeightOf(expected));
        }
        finally
        {
            TtfFontLoader.Unload(expected);
        }
    }

    [Fact]
    public void FontSize_SameFileAndSizeTwice_SharesOneCachedFont()
    {
        string path = FontPath("OpenSans-Regular.ttf");

        TextRuntime first = CreateText(path, 30);
        TextRuntime second = CreateText(path, 30);

        FontOf(second).Texture.Id.ShouldBe(FontOf(first).Texture.Id);
    }

    [Fact]
    public void DisposingCachedFont_RemovesItsLineMetrics()
    {
        string path = FontPath("OpenSans-Regular.ttf");
        TextRuntime text = CreateText(path, 36);
        uint textureId = FontOf(text).Texture.Id;
        string key = _cacheKeys[^2];

        LoaderManager.Self.GetDisposable(key).ShouldBeAssignableTo<ManagedFont>()!
            .Font.Texture.Id.ShouldBe(textureId, "the loaded font must be cached so it is disposed");
        RaylibFontMetricsRegistry.IsRegistered(textureId).ShouldBeTrue();

        LoaderManager.Self.Dispose(key);

        RaylibFontMetricsRegistry.IsRegistered(textureId).ShouldBeFalse();
    }

    [Fact]
    public void FontSize_MissingCustomFntFallingBackToFontFile_EachLoadsAtItsOwnSize()
    {
        string path = FontPath("OpenSans-Regular.ttf");

        TextRuntime small = CreateTextWithMissingCustomFnt(path, 12);
        TextRuntime large = CreateTextWithMissingCustomFnt(path, 48);

        FontOf(small).BaseSize.ShouldBe(12);
        FontOf(large).BaseSize.ShouldBe(48);
    }

    private TextRuntime CreateTextWithMissingCustomFnt(string fontPath, float fontSize)
    {
        string missingFnt = "Missing_" + Guid.NewGuid().ToString("N") + ".fnt";
        TextRuntime text = new();
        text.Font = fontPath;
        text.FontSize = fontSize;
        text.UseCustomFont = true;
        text.CustomFontFile = missingFnt;
        text.Text = "HHHH";
        _cacheKeys.Add(FileManager.Standardize(missingFnt, preserveCase: true, makeAbsolute: true));
        _cacheKeys.Add(FileManager.Standardize(
            text.GetFontCacheFileName(null), preserveCase: true, makeAbsolute: true));
        _cacheKeys.Add(FileManager.Standardize(fontPath, preserveCase: true, makeAbsolute: true));
        return text;
    }

    private TextRuntime CreateText(string fontPath, float fontSize)
    {
        TextRuntime text = new();
        text.Font = fontPath;
        text.FontSize = fontSize;
        text.Text = "HHHH";
        _cacheKeys.Add(FileManager.Standardize(
            text.GetFontCacheFileName(fontPath), preserveCase: true, makeAbsolute: true));
        // The family-keyed entry the fallback used before #6591.
        _cacheKeys.Add(FileManager.Standardize(fontPath, preserveCase: true, makeAbsolute: true));
        return text;
    }

    private static string FontPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", fileName);

    private static Font FontOf(TextRuntime text) => ((Text)text.RenderableComponent!).Font;

    private static float CapHeight(Font font)
    {
        return Raylib.GetGlyphAtlasRec(font, 'H').Height;
    }

    private static float LineHeightOf(Font font)
    {
        TextRuntime runtime = new();
        Text text = (Text)runtime.RenderableComponent!;
        text.Font = font;
        return text.LineHeightInPixels;
    }
}
