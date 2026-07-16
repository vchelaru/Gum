using Shouldly;
using SkiaGum.Content.Fonts;
using SkiaSharp;
using System.IO;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Content;

/// <summary>
/// Pins #3670/#3703: Skia resolves a custom .ttf/.otf font file to a loaded SKTypeface via
/// GumFontMapper, instead of the previous total no-op (Skia never had a custom-font path).
/// </summary>
public class GumFontMapperTests
{
    private static readonly string FixtureTtfPath =
        Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "TestFont.ttf");

    [Fact]
    public void RegisterFontFile_WithValidTtf_ReturnsNonNullFamilyKey()
    {
        string? familyKey = GumFontMapper.RegisterFontFile(FixtureTtfPath);

        familyKey.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void RegisterFontFile_CalledTwice_ReturnsSameFamilyKey()
    {
        string? first = GumFontMapper.RegisterFontFile(FixtureTtfPath);
        string? second = GumFontMapper.RegisterFontFile(FixtureTtfPath);

        second.ShouldBe(first);
    }

    [Fact]
    public void RegisterFontFile_WithMissingFile_ReturnsNull()
    {
        string? familyKey = GumFontMapper.RegisterFontFile(
            Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "DoesNotExist.ttf"));

        familyKey.ShouldBeNull();
    }

    [Fact]
    public void ResolveFontFamily_UseCustomFontWithTtf_ReturnsRegisteredKey()
    {
        string? resolved = GumFontMapper.ResolveFontFamily(
            useCustomFont: true, customFontFile: FixtureTtfPath, font: "Arial");

        resolved.ShouldBe(GumFontMapper.RegisterFontFile(FixtureTtfPath));
    }

    [Fact]
    public void ResolveFontFamily_NotUsingCustomFont_ReturnsFontFamilyNameUnchanged()
    {
        string? resolved = GumFontMapper.ResolveFontFamily(
            useCustomFont: false, customFontFile: FixtureTtfPath, font: "Arial");

        resolved.ShouldBe("Arial");
    }

    [Fact]
    public void ResolveFontFamily_FontAsTtfPath_ReturnsRegisteredKey()
    {
        string? resolved = GumFontMapper.ResolveFontFamily(
            useCustomFont: false, customFontFile: null, font: FixtureTtfPath);

        resolved.ShouldBe(GumFontMapper.RegisterFontFile(FixtureTtfPath));
    }

    [Fact]
    public void RegisterFontFile_WithRelativePath_ResolvesAgainstCurrentFileManagerRelativeDirectory()
    {
        // Pins the exact bug that shipped in the SilkNetGumSample manual-test demo: a relative
        // CustomFontFile path resolves against FileManager.RelativeDirectory AT THE TIME OF THE
        // CALL, not the exe's own directory. RelativeDirectory changes when a .gumx project loads
        // (MonoGameGum/GumService.cs sets it to the project's folder) -- a relative path bundled
        // next to the exe silently fails to resolve once that happens (falls back to the default
        // font with no error), unless the file actually lives under the new RelativeDirectory.
        string previousRelativeDirectory = ToolsUtilities.FileManager.RelativeDirectory;
        try
        {
            ToolsUtilities.FileManager.RelativeDirectory =
                Path.Combine(AppContext.BaseDirectory, "Assets") + Path.DirectorySeparatorChar;

            string? familyKey = GumFontMapper.RegisterFontFile("Fonts/TestFont.ttf");

            familyKey.ShouldNotBeNullOrEmpty();
        }
        finally
        {
            ToolsUtilities.FileManager.RelativeDirectory = previousRelativeDirectory;
        }
    }

    [Fact]
    public void RegisterFontFile_WithRelativePath_WhenRelativeDirectoryDoesNotContainFile_ReturnsNull()
    {
        string previousRelativeDirectory = ToolsUtilities.FileManager.RelativeDirectory;
        try
        {
            ToolsUtilities.FileManager.RelativeDirectory = AppContext.BaseDirectory;

            // No "Fonts" subfolder directly under AppContext.BaseDirectory -- only under Assets/Fonts.
            string? familyKey = GumFontMapper.RegisterFontFile("Fonts/TestFont.ttf");

            familyKey.ShouldBeNull();
        }
        finally
        {
            ToolsUtilities.FileManager.RelativeDirectory = previousRelativeDirectory;
        }
    }

    [Fact]
    public void ResolveFontFamily_UseCustomFontWithNonTtfPath_ReturnsNull()
    {
        // Skia has no bitmap-font atlas renderer, so a .fnt CustomFontFile (the XNA-like/raylib
        // format) isn't supported here -- unsupported, not silently wrong.
        string? resolved = GumFontMapper.ResolveFontFamily(
            useCustomFont: true, customFontFile: "Fonts/Prebaked.fnt", font: "Arial");

        resolved.ShouldBeNull();
    }

    // Typeface (issue #3708): registers an already-loaded SKTypeface directly (as opposed to
    // RegisterFontFile, which loads one from a path), for Text.Typeface -- the explicit-font-object
    // escape hatch Skia lacked entirely, mirroring XNALIKE's BitmapFont / Raylib's Typeface.

    [Fact]
    public void RegisterTypeface_ReturnsNonNullFamilyKey()
    {
        SKTypeface typeface = SKTypeface.FromFile(FixtureTtfPath);

        string familyKey = GumFontMapper.RegisterTypeface(typeface);

        familyKey.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void RegisterTypeface_CalledTwiceWithSameInstance_ReturnsSameFamilyKey()
    {
        SKTypeface typeface = SKTypeface.FromFile(FixtureTtfPath);

        string first = GumFontMapper.RegisterTypeface(typeface);
        string second = GumFontMapper.RegisterTypeface(typeface);

        second.ShouldBe(first);
    }

    [Fact]
    public void RegisterTypeface_CalledWithDifferentInstances_ReturnsDifferentFamilyKeys()
    {
        SKTypeface first = SKTypeface.FromFile(FixtureTtfPath);
        SKTypeface second = SKTypeface.FromFile(FixtureTtfPath);

        string firstKey = GumFontMapper.RegisterTypeface(first);
        string secondKey = GumFontMapper.RegisterTypeface(second);

        secondKey.ShouldNotBe(firstKey);
    }

    [Fact]
    public void TypefaceFromStyle_ResolvesRegisteredTypefaceKey_ReturnsSameInstance()
    {
        SKTypeface typeface = SKTypeface.FromFile(FixtureTtfPath);
        string familyKey = GumFontMapper.RegisterTypeface(typeface);
        GumFontMapper mapper = new();
        Style style = new() { FontFamily = familyKey };

        SKTypeface resolved = mapper.TypefaceFromStyle(style, ignoreFontVariants: false);

        resolved.ShouldBeSameAs(typeface);
    }

    // Embedded font bytes (#3671): registers raw TTF bytes under a family name plus an optional style
    // slot ("Bold"/"Italic"/"BoldItalic"/null), mirroring the KernSmith RegisterFont(familyName, fontData,
    // style) surface XNA-like/raylib themes call through Gum.Themes.ThemePlatform.RegisterFont -- so a
    // theme's embedded fonts resolve on Skia through the same family+style vocabulary, instead of the
    // path/instance-only registries RegisterFontFile/RegisterTypeface offer.

    [Fact]
    public void RegisterFont_WithValidBytes_ReturnsNonNullTypeface()
    {
        byte[] fontBytes = File.ReadAllBytes(FixtureTtfPath);

        SKTypeface? typeface = GumFontMapper.RegisterFont("RegisterFontValidFamily", fontBytes);

        typeface.ShouldNotBeNull();
    }

    [Fact]
    public void RegisterFont_WithInvalidBytes_ReturnsNull()
    {
        byte[] invalidBytes = new byte[] { 1, 2, 3, 4, 5 };

        SKTypeface? typeface = GumFontMapper.RegisterFont("RegisterFontInvalidFamily", invalidBytes);

        typeface.ShouldBeNull();
    }

    [Theory]
    [InlineData(600)] // TextRuntime.IsBold on Skia sets BoldWeight=1.5 -> Style.FontWeight 400*1.5=600
    [InlineData(700)] // The [IsBold] BBCode tag sets Style.FontWeight directly to 700
    public void TypefaceFromStyle_WithBoldWeight_ResolvesRegisteredBoldCut(int boldFontWeight)
    {
        string family = $"TypefaceFromStyleBoldFamily{boldFontWeight}";
        byte[] fontBytes = File.ReadAllBytes(FixtureTtfPath);
        SKTypeface? regularTypeface = GumFontMapper.RegisterFont(family, fontBytes, style: null);
        SKTypeface? boldTypeface = GumFontMapper.RegisterFont(family, fontBytes, style: "Bold");
        GumFontMapper mapper = new();
        Style regularStyle = new() { FontFamily = family, FontWeight = 400, FontItalic = false };
        Style boldStyle = new() { FontFamily = family, FontWeight = boldFontWeight, FontItalic = false };

        SKTypeface resolvedRegular = mapper.TypefaceFromStyle(regularStyle, ignoreFontVariants: false);
        SKTypeface resolvedBold = mapper.TypefaceFromStyle(boldStyle, ignoreFontVariants: false);

        resolvedRegular.ShouldBeSameAs(regularTypeface);
        resolvedBold.ShouldBeSameAs(boldTypeface);
        resolvedBold.ShouldNotBeSameAs(resolvedRegular);
    }

    [Fact]
    public void TypefaceFromStyle_WithoutMatchingStyleSlotRegistered_FallsBackToDefaultCut()
    {
        string family = "TypefaceFromStyleFallbackFamily";
        byte[] fontBytes = File.ReadAllBytes(FixtureTtfPath);
        SKTypeface? regularTypeface = GumFontMapper.RegisterFont(family, fontBytes, style: null);
        GumFontMapper mapper = new();
        Style boldStyle = new() { FontFamily = family, FontWeight = 700, FontItalic = false };

        SKTypeface resolved = mapper.TypefaceFromStyle(boldStyle, ignoreFontVariants: false);

        resolved.ShouldBeSameAs(regularTypeface);
    }
}
