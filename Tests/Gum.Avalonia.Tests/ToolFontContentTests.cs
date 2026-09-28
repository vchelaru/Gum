using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Gum.Managers;
using Shouldly;
using TextureCoordinateSelectionPlugin.RegionSelection;
using XnaAndWinforms;
using Xunit;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The head's csproj links the tool's own bitmap fonts into Content by file name, and the code
/// loads them by a second copy of that name; a rename on one side leaves the rulers or the
/// wireframe's fallback text without a font (#5430).
/// </summary>
public class ToolFontContentTests
{
    [Theory]
    [InlineData(ToolFontService.FontFileRelativePath)]
    [InlineData("Content/TestFont.fnt")]
    public void ShippedFont_ShouldExistWithItsPages_AndNotBeRenderedFromArial(string relativePath)
    {
        string fntPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        File.Exists(fntPath).ShouldBeTrue(fntPath);

        string[] lines = File.ReadAllLines(fntPath);
        lines[0].ShouldNotContain("face=\"Arial\"");

        string[] pages = lines
            .Where(line => line.StartsWith("page ", StringComparison.Ordinal))
            .Select(line => line.Substring(line.IndexOf("file=\"", StringComparison.Ordinal) + 6).TrimEnd('"'))
            .ToArray();
        pages.ShouldNotBeEmpty();
        foreach (string page in pages)
        {
            File.Exists(Path.Combine(Path.GetDirectoryName(fntPath)!, page)).ShouldBeTrue(page);
        }
    }

    // The Texture Coordinates tab extracts this font from XnaAndWinforms.dll by resource name.
    [Theory]
    [InlineData(ImageRegionSelectionCore.EmbeddedFontResourceName)]
    [InlineData(ImageRegionSelectionCore.EmbeddedFontPageResourceName)]
    public void EmbeddedTextureTabFont_ShouldExist_AndNotBeRenderedFromArial(string resourceName)
    {
        Assembly assembly = typeof(RenderTargetFrameLoop).Assembly;

        assembly.GetManifestResourceNames().ShouldContain(resourceName);
        resourceName.ShouldNotContain("Arial");
    }
}
