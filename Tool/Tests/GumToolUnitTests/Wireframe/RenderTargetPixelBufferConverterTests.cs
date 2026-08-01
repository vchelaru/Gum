using Microsoft.Xna.Framework.Graphics;
using Shouldly;
using XnaAndWinforms;
using Xunit;

namespace GumToolUnitTests.Wireframe;

public class RenderTargetPixelBufferConverterTests
{
    [Fact]
    public void GetStrategyForBgraDestination_Bgra32Source_ReturnsDirectCopy()
    {
        RenderTargetPixelBufferConverter.GetStrategyForBgraDestination(SurfaceFormat.Bgra32)
            .ShouldBe(PixelBufferConversionStrategy.DirectCopy);
    }

    [Fact]
    public void GetStrategyForBgraDestination_ColorSource_ReturnsByteSwapRgbaToBgra()
    {
        RenderTargetPixelBufferConverter.GetStrategyForBgraDestination(SurfaceFormat.Color)
            .ShouldBe(PixelBufferConversionStrategy.ByteSwapRgbaToBgra);
    }

    [Fact]
    public void GetStrategyForBgraDestination_UnsupportedSourceFormat_ReturnsNull()
    {
        RenderTargetPixelBufferConverter.GetStrategyForBgraDestination(SurfaceFormat.Rgba64)
            .ShouldBeNull();
    }
}
