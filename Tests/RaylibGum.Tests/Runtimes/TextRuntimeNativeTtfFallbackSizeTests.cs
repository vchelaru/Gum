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

    [Fact]
    public void CreatorWiredLater_ReplacesAndDisposesTheNativeFallbackFont()
    {
        string path = FontPath("OpenSans-Regular.ttf");
        TextRuntime native = CreateText(path, 41);
        string key = _cacheKeys[^2];
        uint nativeId = FontOf(native).Texture.Id;
        RaylibFontMetricsRegistry.IsRegistered(nativeId).ShouldBeTrue();

        FreshFontCreator creator = new(path);
        CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
        TextRuntime created = CreateText(path, 41);

        int callsAfterReplace = creator.Calls;
        callsAfterReplace.ShouldBeGreaterThan(0);
        FontOf(created).Texture.Id.ShouldBe(creator.LastFont.Texture.Id);
        LoaderManager.Self.GetDisposable(key).ShouldBeAssignableTo<ManagedFont>()!
            .Font.Texture.Id.ShouldBe(creator.LastFont.Texture.Id);
        RaylibFontMetricsRegistry.IsRegistered(nativeId).ShouldBeFalse("the replaced native font must be disposed");

        CreateText(path, 41);
        creator.Calls.ShouldBe(callsAfterReplace, "the creator's font is reused once cached");
    }

    [Fact]
    public void CustomContentLoader_IsAskedForTheFamilyBeforeTheFontFileLookup()
    {
        string path = FontPath("OpenSans-Regular.ttf");
        Font customFont = TtfFontLoader.LoadFont(path, 20);
        IContentLoader? previousLoader = LoaderManager.Self.ContentLoader;
        RecordingContentLoader loader = new(previousLoader!, path, customFont);
        LoaderManager.Self.ContentLoader = loader;
        try
        {
            TextRuntime text = CreateText(path, 33);

            loader.FontRequests.ShouldContain(path);
            FontOf(text).Texture.Id.ShouldBe(customFont.Texture.Id);
        }
        finally
        {
            LoaderManager.Self.ContentLoader = previousLoader;
            TtfFontLoader.Unload(customFont);
        }
    }

    // Returns a new font on every call; handing out one font twice would unload it twice.
    private sealed class FreshFontCreator : IRaylibFontCreator
    {
        private readonly string _path;
        public FreshFontCreator(string path) => _path = path;
        public int Calls { get; private set; }
        public Font LastFont { get; private set; }

        public Font? TryCreateFont(RenderingLibrary.Graphics.Fonts.BmfcSave bmfcSave)
        {
            Calls++;
            LastFont = TtfFontLoader.LoadFont(_path, (int)bmfcSave.FontSize);
            return LastFont;
        }
    }

    // Supplies its own font for one family name and delegates everything else to the built-in loader.
    private sealed class RecordingContentLoader : IContentLoader
    {
        private readonly IContentLoader _inner;
        private readonly string _family;
        private readonly Font _font;
        public List<string> FontRequests { get; } = new();

        public RecordingContentLoader(IContentLoader inner, string family, Font font)
        {
            _inner = inner;
            _family = family;
            _font = font;
        }

        public T? LoadContent<T>(string contentName)
        {
            if (typeof(T) == typeof(Font))
            {
                FontRequests.Add(contentName);
                if (contentName == _family)
                {
                    return (T)(object)_font;
                }
            }
            return _inner.LoadContent<T>(contentName);
        }

        public T? TryLoadContent<T>(string contentName) => LoadContent<T>(contentName);
    }

    private TextRuntime CreateText(string fontPath, float fontSize)
    {
        TextRuntime text = new();
        text.Font = fontPath;
        // Setting Font first loads it at the default size too.
        string defaultSizeKey = FileManager.Standardize(
            text.GetFontCacheFileName(fontPath), preserveCase: true, makeAbsolute: true);
        text.FontSize = fontSize;
        text.Text = "HHHH";
        _cacheKeys.Add(defaultSizeKey);
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
