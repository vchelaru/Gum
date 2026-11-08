#if STRIDE
#nullable enable

using SkiaGum.Helpers;
using Stride.Graphics;

namespace Gum;

/// <summary>
/// Gum's view of a SkiaGameRendering <c>SkiaRenderTarget2D</c>. The D3D11, D3D12 and Vulkan
/// packages each declare a type with the same full name and no shared interface, so each is wrapped
/// behind an extern alias (the three <c>Stride*Target</c> classes) and Gum talks to this.
/// </summary>
internal interface IStrideSkiaTarget : ISkiaHostTarget
{
    /// <summary>Ends the pass and composites the target onto the render target bound on the context.</summary>
    void End(GraphicsContext graphicsContext);
}
#endif
