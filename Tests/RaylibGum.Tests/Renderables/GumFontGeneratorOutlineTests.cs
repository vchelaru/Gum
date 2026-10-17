using System.Linq;
using KernSmith.Gum;
using KernSmith.Output;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Issue #6123 — with font smoothing off, OutlineThickness N must be N crisp pixels (alpha 0 or 255),
/// not N-1 solid rings plus a 50% alpha ring. The fix lives in KernSmith (0.23.1); this pins that
/// <see cref="GumFontGenerator"/> routes UseSmoothing = false through to it.
/// </summary>
public class GumFontGeneratorOutlineTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Generate_WhenSmoothingIsOff_OutlineAlphaIsOnlyZeroOrFull(int outlineThickness)
    {
        BmfcSave bmfcSave = new BmfcSave
        {
            FontName = "Arial",
            FontSize = 16,
            UseSmoothing = false,
            OutlineThickness = outlineThickness,
            Ranges = "76",
        };

        BmFontResult result = GumFontGenerator.Generate(bmfcSave);

        byte[] pixels = result.Pages[0].GetRgbaPixelData();
        bool hasOutline = false;
        for (int i = 3; i < pixels.Length; i += 4)
        {
            byte alpha = pixels[i];
            if (alpha != 0) hasOutline = true;
            (alpha == 0 || alpha == 255).ShouldBeTrue($"pixel {i / 4} has partial alpha {alpha}");
        }

        hasOutline.ShouldBeTrue();
    }

    [Fact]
    public void Generate_WhenSmoothingIsOn_OutlineKeepsSoftEdge()
    {
        BmfcSave bmfcSave = new BmfcSave
        {
            FontName = "Arial",
            FontSize = 16,
            UseSmoothing = true,
            OutlineThickness = 1,
            Ranges = "76",
        };

        BmFontResult result = GumFontGenerator.Generate(bmfcSave);

        byte[] pixels = result.Pages[0].GetRgbaPixelData();
        bool hasPartialAlpha = false;
        for (int i = 3; i < pixels.Length; i += 4)
        {
            if (pixels[i] != 0 && pixels[i] != 255) hasPartialAlpha = true;
        }

        hasPartialAlpha.ShouldBeTrue();
    }
}
