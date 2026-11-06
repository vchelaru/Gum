using System;
using System.IO;
using System.Linq;
using KernSmith;
using KernSmith.Gum;
using KernSmith.Output;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// <see cref="GumFontGenerator.UseFaceCache"/> turns on KernSmith's <see cref="FaceCache"/>. The cache only
/// skips work that does not change between two generations of the same face, so the output must be
/// byte-identical with it on and off. The switch is process-wide, so each test restores it.
/// </summary>
public class GumFontGeneratorFaceCacheTests : IDisposable
{
    private readonly bool _previousUseFaceCache = GumFontGenerator.UseFaceCache;
    private readonly Func<string, Stream>? _previousHook = ToolsUtilities.FileManager.CustomGetStreamFromFile;

    public void Dispose()
    {
        GumFontGenerator.UseFaceCache = _previousUseFaceCache;
        ToolsUtilities.FileManager.CustomGetStreamFromFile = _previousHook;
    }

    [Fact]
    public void Generate_WhenTheFaceCacheAlreadyHoldsTheFont_DoesNotReadTheFontFileAgain()
    {
        int reads = 0;
        ToolsUtilities.FileManager.CustomGetStreamFromFile = path =>
        {
            reads++;
            return File.OpenRead(path);
        };
        BmfcSave Make() => new BmfcSave { FontName = "Orbitron-Black", FontFile = FixtureFontPath, FontSize = 24, UseSmoothing = true, Ranges = "65" };

        GumFontGenerator.UseFaceCache = false;
        GumFontGenerator.UseFaceCache = true; // starts from an empty cache
        GumFontGenerator.Generate(Make());
        GumFontGenerator.Generate(Make());
        GumFontGenerator.Generate(Make());
        reads.ShouldBe(1, "because only the first generation needs the bytes; the next two reuse the cached face");

        reads = 0;
        GumFontGenerator.UseFaceCache = false;
        GumFontGenerator.Generate(Make());
        GumFontGenerator.Generate(Make());
        reads.ShouldBe(2, "because with the cache off every generation reads the file");
    }

    [Fact]
    public void UseFaceCache_DefaultsToOn_BecauseTheseLibrariesAreForGames()
    {
        // The static constructor applies the default; read it before any test here changes it.
        _previousUseFaceCache.ShouldBeTrue();
    }

    [Fact]
    public void UseFaceCache_WritesThroughToKernSmithsFaceCache()
    {
        GumFontGenerator.UseFaceCache = false;
        FaceCache.Enabled.ShouldBeFalse();

        GumFontGenerator.UseFaceCache = true;
        FaceCache.Enabled.ShouldBeTrue();
    }

    [Theory]
    [InlineData(18, false, 0)]
    [InlineData(24, true, 0)]
    [InlineData(32, false, 2)]
    [InlineData(48, true, 3)]
    public void Generate_WithTheFaceCacheOn_IsByteIdenticalToWithItOff(int fontSize, bool isBold, int outline)
    {
        BmfcSave Make() => new BmfcSave
        {
            FontName = "Orbitron-Black",
            FontFile = FixtureFontPath,
            FontSize = fontSize,
            IsBold = isBold,
            OutlineThickness = outline,
            UseSmoothing = true,
            Ranges = "32-126",
        };

        GumFontGenerator.UseFaceCache = false;
        BmFontResult off = GumFontGenerator.Generate(Make());

        GumFontGenerator.UseFaceCache = true;
        BmFontResult firstWithCache = GumFontGenerator.Generate(Make());
        BmFontResult secondWithCache = GumFontGenerator.Generate(Make());

        AssertIdentical(off, firstWithCache);
        AssertIdentical(off, secondWithCache);
    }

    [Fact]
    public void Generate_WithTheFaceCacheOn_StillReflectsADifferentSizeOfTheSameFace()
    {
        GumFontGenerator.UseFaceCache = true;
        BmfcSave Make(int size) => new BmfcSave { FontName = "Orbitron-Black", FontFile = FixtureFontPath, FontSize = size, UseSmoothing = true, Ranges = "65" };

        BmFontResult small = GumFontGenerator.Generate(Make(18));
        BmFontResult large = GumFontGenerator.Generate(Make(48));

        large.Model.Characters.Single().Height.ShouldBeGreaterThan(small.Model.Characters.Single().Height);
    }

    private static void AssertIdentical(BmFontResult expected, BmFontResult actual)
    {
        actual.FntText.ShouldBe(expected.FntText);
        actual.Pages.Count.ShouldBe(expected.Pages.Count);
        for (int i = 0; i < expected.Pages.Count; i++)
        {
            actual.Pages[i].Width.ShouldBe(expected.Pages[i].Width);
            actual.Pages[i].Height.ShouldBe(expected.Pages[i].Height);
            actual.Pages[i].PixelData.SequenceEqual(expected.Pages[i].PixelData).ShouldBeTrue($"page {i} pixels differ");
        }
    }

    private static string FixtureFontPath =>
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");
}
