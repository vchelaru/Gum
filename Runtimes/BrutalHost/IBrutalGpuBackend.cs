#if BRUTAL
#nullable enable

using SkiaGum.Helpers;

namespace Gum;

/// <summary>A Skia target Gum draws into: the package's <c>SkiaRenderTarget2D</c> (Vulkan) or its <c>SkiaDirectCanvas</c> over the default framebuffer (OpenGL). <c>Begin</c> leaves the target transparent, or, for the direct canvas, untouched so Gum composites over the game's frame.</summary>
internal interface IBrutalTarget : ISkiaHostTarget
{
    /// <summary>Ends the pass and flushes Skia's work.</summary>
    void End();
}

/// <summary>
/// What differs between Gum.Brutal.OGL (OpenGL) and Gum.Brutal.Vulkan: owning the SkiaGameRendering context,
/// creating targets over it, and putting a finished target on the window. Tests supply a fake.
/// </summary>
internal interface IBrutalGpuBackend : System.IDisposable
{
    /// <summary>Creates a render target of the given size on the backend's context.</summary>
    IBrutalTarget CreateTarget(int width, int height);

    /// <summary>Blocks until the GPU has finished every frame already submitted. Called before the current target is replaced, so its memory is not freed while a presented frame still samples it.</summary>
    void WaitForGpu();

    /// <summary>Shows <paramref name="target"/>, which has just ended its pass, on the window.</summary>
    void Present(IBrutalTarget target, int width, int height);
}
#endif
