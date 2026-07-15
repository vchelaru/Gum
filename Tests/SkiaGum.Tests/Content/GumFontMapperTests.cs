using Shouldly;
using SkiaGum.Content.Fonts;
using System.IO;

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
}
