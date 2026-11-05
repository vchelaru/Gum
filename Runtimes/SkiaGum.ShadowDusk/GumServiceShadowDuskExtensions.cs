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
    /// A shader ShadowDusk does not read, including SkSL, keeps loading the way it did before this call.
    /// </summary>
    public static void UseShadowDusk(this GumServiceSkiaBase gumService)
    {
        Func<ShaderSource, SKRuntimeEffect?>? previousResolver = CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver;

        // ShadowDusk goes first and decides from the content whether it reads the shader. SkiaGum's default
        // resolver throws for anything it does not recognize, so it can only run after ShadowDusk declines.
        // A source that cannot be read cannot be decided about either, so the previous resolver gets it.
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = source =>
        {
            try
            {
                return ShadowDuskEffectLoader.TryLoad(source) ?? previousResolver?.Invoke(source);
            }
            catch (System.IO.IOException) when (previousResolver != null)
            {
                return previousResolver(source);
            }
        };
    }
}
