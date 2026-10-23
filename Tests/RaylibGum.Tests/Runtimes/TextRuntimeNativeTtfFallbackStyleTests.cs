using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum.Renderables;
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
/// #6620: with no font creator, a system family name resolves to the cut matching IsBold/IsItalic, as Skia's
/// font manager does; a font file path loads that file whatever the style, as Skia does.
/// </summary>
public class TextRuntimeNativeTtfFallbackStyleTests : BaseTestClass
{
    private const string Family = "Nunito";

    private readonly IRaylibFontCreator? _previousCreator;
    private readonly bool _previousCacheTextures;
    private readonly string _fontDirectory;
    private readonly List<string> _cacheKeys = new();

    public TextRuntimeNativeTtfFallbackStyleTests()
    {
        _previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
        _previousCacheTextures = LoaderManager.Self.CacheTextures;
        LoaderManager.Self.CacheTextures = true;

        // The regular cut is named as the family-name lookup expects; the other cuts keep their own names.
        _fontDirectory = Path.Combine(Path.GetTempPath(), "Gum6620_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_fontDirectory);
        File.Copy(NunitoGlyphSignature.PathOf("Nunito-Regular.ttf"), Path.Combine(_fontDirectory, Family + ".ttf"));
        foreach (string cut in new[] { "Nunito-Bold.ttf", "Nunito-Italic.ttf", "Nunito-BoldItalic.ttf" })
        {
            File.Copy(NunitoGlyphSignature.PathOf(cut), Path.Combine(_fontDirectory, cut));
        }
        ContentLoader.SystemFontDirectoryOverride = _fontDirectory;
    }

    public override void Dispose()
    {
        foreach (string key in _cacheKeys)
        {
            LoaderManager.Self.Dispose(key);
        }
        ContentLoader.SystemFontDirectoryOverride = null;
        LoaderManager.Self.CacheTextures = _previousCacheTextures;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = _previousCreator;
        try
        {
            Directory.Delete(_fontDirectory, recursive: true);
        }
        catch (IOException)
        {
            // A font file still open in a native handle is left for the OS temp cleanup.
        }
        base.Dispose();
    }

    [Theory]
    [InlineData(false, false, "Nunito-Regular.ttf")]
    [InlineData(true, false, "Nunito-Bold.ttf")]
    [InlineData(false, true, "Nunito-Italic.ttf")]
    [InlineData(true, true, "Nunito-BoldItalic.ttf")]
    public void SystemFamily_LoadsTheCutMatchingBoldAndItalic(bool isBold, bool isItalic, string expectedCut)
    {
        TextRuntime text = CreateText(Family, 32, isBold, isItalic);

        NunitoGlyphSignature.Of(FontOf(text)).ShouldBe(NunitoGlyphSignature.OfFile(NunitoGlyphSignature.PathOf(expectedCut), 32));
    }

    [Fact]
    public void FontFilePath_IsBold_LoadsThatFileUnchanged()
    {
        string regularPath = NunitoGlyphSignature.PathOf("Nunito-Regular.ttf");

        TextRuntime text = CreateText(regularPath, 32, isBold: true, isItalic: false);

        NunitoGlyphSignature.Of(FontOf(text)).ShouldBe(NunitoGlyphSignature.OfFile(regularPath, 32));
    }

    [Fact]
    public void SystemFamily_BoldCutMissing_LoadsTheRegularCut()
    {
        File.Delete(Path.Combine(_fontDirectory, "Nunito-Bold.ttf"));

        TextRuntime text = CreateText(Family, 32, isBold: true, isItalic: false);

        NunitoGlyphSignature.Of(FontOf(text)).ShouldBe(NunitoGlyphSignature.OfFile(NunitoGlyphSignature.PathOf("Nunito-Regular.ttf"), 32));
    }

    private TextRuntime CreateText(string font, float fontSize, bool isBold, bool isItalic)
    {
        TextRuntime text = new();
        text.Font = font;
        _cacheKeys.Add(CacheKeyOf(text, font));
        text.FontSize = fontSize;
        _cacheKeys.Add(CacheKeyOf(text, font));
        text.IsBold = isBold;
        _cacheKeys.Add(CacheKeyOf(text, font));
        text.IsItalic = isItalic;
        _cacheKeys.Add(CacheKeyOf(text, font));
        text.Text = "HHHH";
        _cacheKeys.Add(FileManager.Standardize(font, preserveCase: true, makeAbsolute: true));
        return text;
    }

    private static string CacheKeyOf(TextRuntime text, string font)
    {
        string? fontFilePath = File.Exists(font) ? font : null;
        return FileManager.Standardize(text.GetFontCacheFileName(fontFilePath), preserveCase: true, makeAbsolute: true);
    }

    private static Font FontOf(TextRuntime text) => ((Text)text.RenderableComponent!).Font;
}
