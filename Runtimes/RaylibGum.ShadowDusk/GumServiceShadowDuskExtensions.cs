using System;
using RaylibGum.Renderables;

namespace Gum;

/// <summary>
/// Adds <c>.fx</c> and <c>.slang</c> shader loading to a render-target container's
/// <c>SourceShaderFile</c>.
/// </summary>
public static class GumServiceShadowDuskExtensions
{
    /// <summary>
    /// Lets <c>SourceShaderFile</c> point at an <c>.fx</c> or <c>.slang</c> file, which ShadowDusk converts to a
    /// GLSL fragment shader at runtime. The defaults the shader declares (<c>float Thickness = 1.0;</c>) are
    /// registered too, so a container that does not set a parameter draws with its declared default. Call
    /// once at startup, after the window exists. A shader ShadowDusk does not read keeps loading through
    /// whatever resolver was registered before this call.
    /// </summary>
    /// <returns><paramref name="gumService"/>, so calls chain.</returns>
    public static T UseShadowDusk<T>(this T gumService) where T : GumService
    {
        if (gumService == null)
        {
            throw new ArgumentNullException(nameof(gumService));
        }

        ShadowDuskShaderLoader loader = new ShadowDuskShaderLoader();
        Func<ShaderSource, object?>? previousResolver = CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver;

        // ShadowDusk goes first and decides from the content whether it reads the shader; an input it does not
        // read falls through to the resolver registered before this call. A Shader is a struct, so the
        // loader's result is boxed into the resolver's object? return.
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = source =>
        {
            Raylib_cs.Shader? shader;
            try
            {
                shader = loader.TryLoad(source);
            }
            catch (System.IO.IOException) when (previousResolver != null)
            {
                return previousResolver(source);
            }

            return shader.HasValue ? (object)shader.Value : previousResolver?.Invoke(source);
        };

        return gumService;
    }
}
