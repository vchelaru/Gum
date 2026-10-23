using Gum.GueDeriving;
using Gum.Wireframe;
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
        int headerSize = 12 + 4 * faceFileNames.Length;
        byte[] header = new byte[headerSize];
        "ttcf"u8.CopyTo(header);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), 0x00010000);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8), (uint)faceFileNames.Length);

        using MemoryStream output = new();
        output.Write(header);
        for (int i = 0; i < faceFileNames.Length; i++)
        {
            // Each file is embedded whole, so its table offsets move by where it starts.
            long start = output.Position;
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(12 + 4 * i), (uint)start);
            byte[] font = File.ReadAllBytes(NunitoGlyphSignature.PathOf(faceFileNames[i]));
            int tableCount = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(4));
            for (int table = 0; table < tableCount; table++)
            {
                Span<byte> offset = font.AsSpan(12 + 16 * table + 8);
                BinaryPrimitives.WriteUInt32BigEndian(offset, BinaryPrimitives.ReadUInt32BigEndian(offset) + (uint)start);
            }
            output.Write(font);
        }
        output.Position = 0;
        output.Write(header);
        File.WriteAllBytes(path, output.ToArray());
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
