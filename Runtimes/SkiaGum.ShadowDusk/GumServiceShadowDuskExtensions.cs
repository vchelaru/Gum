using System;
using SkiaGum;
using SkiaSharp;

namespace Gum;

/// <summary>
/// Adds <c>.fx</c> and <c>.slang</c> shader loading to a SkiaGum render-target container's
/// <c>SourceShaderFile</c>.
/// </summary>
public static class GumServiceShadowDuskExtensions
{
    /// <summary>
    /// Lets <c>SourceShaderFile</c> point at an <c>.fx</c> or <c>.slang</c> file. Call once at startup.
    /// Other extensions, including <c>.sksl</c>, keep loading the way they did before this call.
    /// </summary>
    public static void UseShadowDusk(this GumServiceSkiaBase gumService)
    {
        Func<string, SKRuntimeEffect?>? previousResolver = CustomSetPropertyOnRenderable.RenderTargetEffectResolver;

        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path =>
            ShadowDuskEffectLoader.CanLoad(path)
                ? ShadowDuskEffectLoader.Load(path)
                : previousResolver?.Invoke(path);
    }
}
