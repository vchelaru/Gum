#if BRUTAL
#nullable enable

using SkiaGum.Helpers;

namespace Gum;

/// <summary>A Skia render target Gum draws into, wrapped over the package's <c>SkiaRenderTarget2D</c>.</summary>
internal interface IBrutalTarget : ISkiaHostTarget
{
    /// <summary>Ends the pass and flushes Skia's work so the target can be sampled.</summary>
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
