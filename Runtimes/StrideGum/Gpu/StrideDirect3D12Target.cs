#if STRIDE_GPU_WINDOWS
extern alias SgrD3D12;

#nullable enable

using SkiaSharp;
using Stride.Graphics;
using Target = SgrD3D12::SkiaGameRendering.SkiaRenderTarget2D;

namespace Gum;

/// <summary><see cref="IStrideSkiaTarget"/> over SkiaGameRendering.Stride.D3D12.</summary>
internal sealed class StrideDirect3D12Target : IStrideSkiaTarget
{
    private readonly Target _target;

    internal StrideDirect3D12Target(GraphicsDevice graphicsDevice, int width, int height) =>
        _target = new Target(graphicsDevice, width, height);

    public SKCanvas Canvas => _target.Canvas;

    public void Begin() => _target.Begin();

    public void End(GraphicsContext graphicsContext) => _target.End(graphicsContext);

    public void EndWithoutDrawing() => _target.EndWithoutDrawing();

    public void Dispose() => _target.Dispose();
}
#endif
