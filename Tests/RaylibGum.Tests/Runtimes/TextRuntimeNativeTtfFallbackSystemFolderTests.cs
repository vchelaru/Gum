using Gum.GueDeriving;
using Gum.Wireframe;
using GumTestSupport;
using Raylib_cs;
using RaylibGum.Renderables;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// #6639: with no font creator, a system family is found by the family name inside the font files (in any
/// subfolder, whatever the file is called, and in .ttc/.otc collections), not only by a file named after it.
/// </summary>
public class TextRuntimeNativeTtfFallbackSystemFolderTests : BaseTestClass
{
    private const string Family = "Nunito";

    private readonly IRaylibFontCreator? _previousCreator;
    private readonly bool _previousCacheTextures;
    private readonly string _fontDirectory;
    private readonly List<string> _cacheKeys = new();

    public TextRuntimeNativeTtfFallbackSystemFolderTests()
    {
        _previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
        _previousCacheTextures = LoaderManager.Self.CacheTextures;
        LoaderManager.Self.CacheTextures = true;

        _fontDirectory = Path.Combine(Path.GetTempPath(), "Gum6639_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_fontDirectory);
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

    [Fact]
    public void SystemFamily_RegularFileInSubfolderNotNamedAfterFamily_IsFound()
    {
        Directory.CreateDirectory(Path.Combine(_fontDirectory, "nunito"));
        File.Copy(NunitoGlyphSignature.PathOf("Nunito-Regular.ttf"), Path.Combine(_fontDirectory, "nunito", "FontFile1.ttf"));

        TextRuntime text = CreateText(Family, isBold: false, isItalic: false);

        NunitoGlyphSignature.Of(FontOf(text)).ShouldBe(NunitoGlyphSignature.OfFile(NunitoGlyphSignature.PathOf("Nunito-Regular.ttf"), 32));
    }

    [Theory]
    [InlineData(false, false, "Nunito-Regular.ttf")]
    [InlineData(true, false, "Nunito-Bold.ttf")]
    [InlineData(false, true, "Nunito-Italic.ttf")]
    [InlineData(true, true, "Nunito-BoldItalic.ttf")]
    public void SystemFamily_FacesInAFontCollection_LoadTheCutMatchingBoldAndItalic(bool isBold, bool isItalic, string expectedCut)
    {
        WriteCollection(Path.Combine(_fontDirectory, "Faces.ttc"),
            "Nunito-Italic.ttf", "Nunito-BoldItalic.ttf", "Nunito-Regular.ttf", "Nunito-Bold.ttf");

        TextRuntime text = CreateText(Family, isBold, isItalic);

        NunitoGlyphSignature.Of(FontOf(text)).ShouldBe(NunitoGlyphSignature.OfFile(NunitoGlyphSignature.PathOf(expectedCut), 32));
    }

    [Fact]
    public void FontFilePath_Collection_LoadsItsFirstFace()
    {
        string collectionPath = Path.Combine(_fontDirectory, "Faces.ttc");
        WriteCollection(collectionPath, "Nunito-Bold.ttf", "Nunito-Regular.ttf");

        TextRuntime text = CreateText(collectionPath, isBold: false, isItalic: false);

        NunitoGlyphSignature.Of(FontOf(text)).ShouldBe(NunitoGlyphSignature.OfFile(NunitoGlyphSignature.PathOf("Nunito-Bold.ttf"), 32));
    }

    [Fact]
    public void TryLoadTtfFont_CollectionDeletedAfterTheFolderWasScanned_ReturnsNull()
    {
        string collectionPath = Path.Combine(_fontDirectory, "Faces.ttc");
        WriteCollection(collectionPath, "Nunito-Regular.ttf");
        SystemFontStyleFinder.Find(_fontDirectory, Family, false, false).ShouldNotBeNull();
        File.Delete(collectionPath);

        Font? font = null;
        Should.NotThrow(() => font = ContentLoader.TryLoadTtfFont(Family, 32));

        font.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnreadableFontData_IsNotLoadedAsRaylibsDefaultFont(bool inCollection)
    {
        if (inCollection)
        {
            File.WriteAllBytes(Path.Combine(_fontDirectory, "Faces.ttc"), FontContainerTestWriter.ToTtc(UnreadableNunito()));
        }
        else
        {
            File.WriteAllBytes(Path.Combine(_fontDirectory, "Broken.ttf"), UnreadableNunito());
        }

        ContentLoader.TryLoadTtfFont(Family, 32).ShouldBeNull();
        _cacheKeys.Add(ContentLoader.StandardizeCaseSensitive(Family));
        LoaderManager.Self.LoadContent<Font>(Family).BaseSize.ShouldBe(0);
    }

    [Fact]
    public void GetSystemFontFolders_ListsTheFoldersEachPlatformKeepsFontsIn()
    {
        // macOS keeps Arial under Supplemental, a subfolder of the first folder, so it is searched with it.
        ContentLoader.GetSystemFontFolders(false, false, true, "/Users/u").ShouldBe(
            new[] { "/System/Library/Fonts", "/Library/Fonts", Path.Combine("/Users/u", "Library/Fonts") });
        ContentLoader.GetSystemFontFolders(false, true, false, "/home/u").ShouldBe(
            new[]
            {
                "/usr/share/fonts", "/usr/local/share/fonts",
                Path.Combine("/home/u", ".local/share/fonts"), Path.Combine("/home/u", ".fonts"),
            });
        ContentLoader.GetSystemFontFolders(true, false, false, "C:/Users/u").ShouldBe(new[] { "C:/Windows/Fonts" });
    }

    /// <summary>
    /// Writes a font collection holding the named Nunito files as its faces, in order.
    /// </summary>
    private static void WriteCollection(string path, params string[] faceFileNames)
    {
        List<byte[]> faces = new();
        foreach (string name in faceFileNames)
        {
            faces.Add(File.ReadAllBytes(NunitoGlyphSignature.PathOf(name)));
        }
        File.WriteAllBytes(path, FontContainerTestWriter.ToTtc(faces));
    }

    /// <summary>
    /// Nunito Regular with its cmap table renamed, which raylib cannot read, so it returns its own default font.
    /// </summary>
    private static byte[] UnreadableNunito()
    {
        byte[] font = File.ReadAllBytes(NunitoGlyphSignature.PathOf("Nunito-Regular.ttf"));
        int tableCount = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(4));
        for (int i = 0; i < tableCount; i++)
        {
            if (System.Text.Encoding.ASCII.GetString(font, 12 + 16 * i, 4) == "cmap")
            {
                font[12 + 16 * i] = (byte)'x';
            }
        }
        return font;
    }

    private TextRuntime CreateText(string font, bool isBold, bool isItalic)
    {
        TextRuntime text = new();
        text.Font = font;
        _cacheKeys.Add(CacheKeyOf(text, font));
        text.FontSize = 32;
        _cacheKeys.Add(CacheKeyOf(text, font));
        text.IsBold = isBold;
        _cacheKeys.Add(CacheKeyOf(text, font));
        text.IsItalic = isItalic;
        _cacheKeys.Add(CacheKeyOf(text, font));
        text.Text = "HHHH";
        return text;
    }

    private static string CacheKeyOf(TextRuntime text, string font)
    {
        string? fontFilePath = File.Exists(font) ? font : null;
        return FileManager.Standardize(text.GetFontCacheFileName(fontFilePath), preserveCase: true, makeAbsolute: true);
    }

    private static Font FontOf(TextRuntime text) => ((Text)text.RenderableComponent!).Font;
}
