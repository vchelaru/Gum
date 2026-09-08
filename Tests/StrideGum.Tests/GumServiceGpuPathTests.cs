using Gum;
using Shouldly;
using Stride.Graphics;

namespace StrideGum.Tests;

/// <summary>
/// Pins <see cref="GumService.ShouldUseGpuPath"/>: the platform check that decides whether
/// <see cref="GumService"/> renders through the zero-copy ANGLE/D3D11 GPU path
/// (<c>SkiaGameRendering.Stride.D3D11</c>) or falls back to the CPU raster-and-upload path (issue
/// #4617). D3D11 is the only backend with an adapter; everything else must fall back.
/// </summary>
public class GumServiceGpuPathTests
{
    [Fact]
    public void ShouldUseGpuPath_ReturnsTrue_ForDirect3D11()
    {
        GumService.ShouldUseGpuPath(GraphicsPlatform.Direct3D11).ShouldBeTrue();
    }

    [Theory]
    [InlineData(GraphicsPlatform.Vulkan)]
    [InlineData(GraphicsPlatform.Direct3D12)]
    [InlineData(GraphicsPlatform.Null)]
    public void ShouldUseGpuPath_ReturnsFalse_ForNonDirect3D11Platforms(GraphicsPlatform platform)
    {
        GumService.ShouldUseGpuPath(platform).ShouldBeFalse();
    }
}
