#if STRIDE
#nullable enable

using SkiaSharp;
using Stride.Graphics;
using System;

namespace Gum;

/// <summary>
/// Gum's view of a SkiaGameRendering <c>SkiaRenderTarget2D</c>. The D3D11, D3D12 and Vulkan
/// packages each declare a type with the same full name and no shared interface, so each is wrapped
/// behind an extern alias (the three <c>Stride*Target</c> classes) and Gum talks to this.
/// </summary>
internal interface IStrideSkiaTarget : IDisposable
{
    /// <summary>The Skia canvas. Only readable between <see cref="Begin"/> and an End call.</summary>
    SKCanvas Canvas { get; }

    /// <summary>Starts a pass and clears the target to transparent.</summary>
    void Begin();

    /// <summary>Ends the pass and composites the target onto the render target bound on the context.</summary>
    void End(GraphicsContext graphicsContext);

    void EndWithoutDrawing();
}
#endif
