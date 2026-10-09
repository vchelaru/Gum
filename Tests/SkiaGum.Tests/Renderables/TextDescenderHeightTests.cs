using RenderingLibrary.Graphics;
using Shouldly;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// <see cref="IText.DescenderHeight"/> is unscaled on every backend; layout multiplies it by
/// <see cref="IText.FontScale"/> itself.
/// </summary>
public class TextDescenderHeightTests
{
    [Fact]
    public void DescenderHeight_ShouldNotIncludeFontScale()
    {
        Text unscaled = new();
        unscaled.FontName = "Arial";
        unscaled.FontSize = 20;
        unscaled.RawText = "Typography";
        Text scaled = new();
        scaled.FontName = "Arial";
        scaled.FontSize = 20;
        scaled.RawText = "Typography";

        scaled.FontScale = 2;

        unscaled.DescenderHeight.ShouldBeGreaterThan(0);
        scaled.DescenderHeight.ShouldBe(unscaled.DescenderHeight, tolerance: 0.01f);
    }
}
