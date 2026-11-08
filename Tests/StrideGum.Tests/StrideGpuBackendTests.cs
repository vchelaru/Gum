using Gum;
using Shouldly;
using Stride.Graphics;

namespace StrideGum.Tests;

/// <summary>
/// Pins <see cref="StrideGpuBackendSelector.Select"/>: Gum renders Stride only through
/// SkiaGameRendering on the GPU, one package per graphics API (D3D11, D3D12, Vulkan). Any other API
/// has no package and must fail loudly instead of falling back to CPU rasterization.
/// </summary>
public class StrideGpuBackendTests
{
    [Theory]
    [InlineData(GraphicsPlatform.Direct3D11, "Direct3D11")]
    [InlineData(GraphicsPlatform.Direct3D12, "Direct3D12")]
    [InlineData(GraphicsPlatform.Vulkan, "Vulkan")]
    public void Select_ReturnsMatchingBackend_ForSupportedPlatforms(GraphicsPlatform platform, string expected)
    {
        StrideGpuBackendSelector.Select(platform).ToString().ShouldBe(expected);
    }

    [Theory]
    [InlineData(GraphicsPlatform.Null)]
    [InlineData((GraphicsPlatform)99)]
    public void Select_Throws_NamingThePlatform_ForUnsupportedPlatforms(GraphicsPlatform platform)
    {
        NotSupportedException exception =
            Should.Throw<NotSupportedException>(() => StrideGpuBackendSelector.Select(platform));

        exception.Message.ShouldContain(platform.ToString());
    }
}
