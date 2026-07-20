using Microsoft.Xna.Framework.Graphics;
using Shouldly;
using System.Drawing.Imaging;
using XnaAndWinforms;
using Xunit;

namespace GumToolUnitTests.Wireframe;

public class RenderTargetPixelBufferConverterTests
{
    [Theory]
    [InlineData(SurfaceFormat.Color, PixelFormat.Format32bppArgb)]
    [InlineData(SurfaceFormat.Color, PixelFormat.Format32bppPArgb)]
    public void GetStrategy_ColorSourceWithArgbDestination_ReturnsByteSwapRgbaToBgra(
        SurfaceFormat sourceFormat, PixelFormat destinationFormat)
    {
        RenderTargetPixelBufferConverter.GetStrategy(sourceFormat, destinationFormat)
            .ShouldBe(PixelBufferConversionStrategy.ByteSwapRgbaToBgra);
    }

    [Theory]
    [InlineData(SurfaceFormat.Bgra32, PixelFormat.Format32bppArgb)]
    [InlineData(SurfaceFormat.Bgra32, PixelFormat.Format32bppPArgb)]
    public void GetStrategy_Bgra32SourceWithArgbDestination_ReturnsDirectCopy(
        SurfaceFormat sourceFormat, PixelFormat destinationFormat)
    {
        RenderTargetPixelBufferConverter.GetStrategy(sourceFormat, destinationFormat)
            .ShouldBe(PixelBufferConversionStrategy.DirectCopy);
    }

    [Fact]
    public void GetStrategy_UnsupportedDestinationFormat_ReturnsNull()
    {
        RenderTargetPixelBufferConverter.GetStrategy(SurfaceFormat.Color, PixelFormat.Format24bppRgb)
            .ShouldBeNull();
    }

    [Fact]
    public void GetStrategy_UnsupportedSourceFormat_ReturnsNull()
    {
        RenderTargetPixelBufferConverter.GetStrategy(SurfaceFormat.Rgba64, PixelFormat.Format32bppArgb)
            .ShouldBeNull();
    }

    [Fact]
    public void GetStrategyForBgraDestination_ColorSource_ReturnsByteSwapRgbaToBgra()
    {
        RenderTargetPixelBufferConverter.GetStrategyForBgraDestination(SurfaceFormat.Color)
            .ShouldBe(PixelBufferConversionStrategy.ByteSwapRgbaToBgra);
    }

    [Fact]
    public void GetStrategyForBgraDestination_Bgra32Source_ReturnsDirectCopy()
    {
        RenderTargetPixelBufferConverter.GetStrategyForBgraDestination(SurfaceFormat.Bgra32)
            .ShouldBe(PixelBufferConversionStrategy.DirectCopy);
    }

    [Fact]
    public void GetStrategyForBgraDestination_UnsupportedSourceFormat_ReturnsNull()
    {
        RenderTargetPixelBufferConverter.GetStrategyForBgraDestination(SurfaceFormat.Rgba64)
            .ShouldBeNull();
    }
}
