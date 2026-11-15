#if STRIDE
#nullable enable

using Stride.Graphics;
using System;

namespace Gum;

/// <summary>
/// The SkiaGameRendering package that draws Gum for a running Stride graphics API. Gum has no CPU
/// path: an API with no SkiaGameRendering package is unsupported.
/// </summary>
internal enum StrideGpuBackend
{
    Direct3D11,
    Direct3D12,
    Vulkan,
}

internal static class StrideGpuBackendSelector
{
    /// <summary>
    /// Maps the API a <see cref="GraphicsDevice"/> runs on to its SkiaGameRendering package
    /// (<c>SkiaGameRendering.Stride.D3D11</c>, <c>.D3D12</c> or <c>.VK</c>).
    /// </summary>
    /// <exception cref="NotSupportedException">The API has no SkiaGameRendering package.</exception>
    internal static StrideGpuBackend Select(GraphicsPlatform platform) => platform switch
    {
        GraphicsPlatform.Direct3D11 => StrideGpuBackend.Direct3D11,
        GraphicsPlatform.Direct3D12 => StrideGpuBackend.Direct3D12,
        GraphicsPlatform.Vulkan => StrideGpuBackend.Vulkan,
        _ => throw new NotSupportedException(
            $"Gum.Stride renders through SkiaGameRendering on the GPU and does not support the " +
            $"'{platform}' graphics API. Supported: Direct3D11, Direct3D12 and Vulkan. " +
            "There is no CPU fallback."),
    };
}
#endif
