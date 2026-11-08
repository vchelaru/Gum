#if STRIDE
extern alias SgrVulkan;

#nullable enable

using SkiaSharp;
using Stride.Graphics;
using Target = SgrVulkan::SkiaGameRendering.SkiaRenderTarget2D;

namespace Gum;

/// <summary><see cref="IStrideSkiaTarget"/> over SkiaGameRendering.Stride.VK.</summary>
internal sealed class StrideVulkanTarget : IStrideSkiaTarget
{
    private readonly Target _target;

    internal StrideVulkanTarget(GraphicsDevice graphicsDevice, int width, int height) =>
        _target = new Target(graphicsDevice, width, height);

    public SKCanvas Canvas => _target.Canvas;

    public void Begin() => _target.Begin();

    public void End(GraphicsContext graphicsContext) => _target.End(graphicsContext);

    public void EndWithoutDrawing() => _target.EndWithoutDrawing();

    public void Dispose() => _target.Dispose();
}
#endif
