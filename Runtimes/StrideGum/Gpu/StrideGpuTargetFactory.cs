#if STRIDE
#nullable enable

using Stride.Graphics;
using System;
using System.Runtime.CompilerServices;

namespace Gum;

internal static class StrideGpuTargetFactory
{
    /// <summary>
    /// Creates a render target on the SkiaGameRendering package for the running graphics API. Each
    /// backend is built in its own non-inlined method so the JIT loads only the package in use (a
    /// game ships the packages for the API it builds against).
    /// </summary>
    /// <exception cref="NotSupportedException">No package for the API, or this build of Gum lacks it.</exception>
    internal static IStrideSkiaTarget Create(GraphicsDevice graphicsDevice, int width, int height)
    {
        StrideGpuBackend backend = StrideGpuBackendSelector.Select(GraphicsDevice.Platform);
        switch (backend)
        {
            case StrideGpuBackend.Vulkan:
                return CreateVulkan(graphicsDevice, width, height);
#if STRIDE_GPU_WINDOWS
            case StrideGpuBackend.Direct3D11:
                return CreateDirect3D11(graphicsDevice, width, height);
            case StrideGpuBackend.Direct3D12:
                return CreateDirect3D12(graphicsDevice, width, height);
#endif
            default:
                throw new NotSupportedException(
                    $"Gum.Stride supports the '{backend}' graphics API only in a project that targets " +
                    "net10.0-windows7.0 (the plain net10.0 build supports Vulkan only). There is no CPU fallback.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IStrideSkiaTarget CreateVulkan(GraphicsDevice device, int width, int height) =>
        new StrideVulkanTarget(device, width, height);

#if STRIDE_GPU_WINDOWS
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IStrideSkiaTarget CreateDirect3D11(GraphicsDevice device, int width, int height) =>
        new StrideDirect3D11Target(device, width, height);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IStrideSkiaTarget CreateDirect3D12(GraphicsDevice device, int width, int height) =>
        new StrideDirect3D12Target(device, width, height);
#endif
}
#endif
