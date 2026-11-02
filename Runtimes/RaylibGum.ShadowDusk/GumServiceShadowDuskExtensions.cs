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
    /// once at startup, after the window exists. Other extensions keep loading through whatever resolver was
    /// registered before this call.
    /// </summary>
    public static void UseShadowDusk(this GumService gumService)
    {
        if (gumService == null)
        {
            throw new ArgumentNullException(nameof(gumService));
        }

        ShadowDuskShaderLoader loader = new ShadowDuskShaderLoader();
        Func<string, object?>? previousResolver = CustomSetPropertyOnRenderable.RenderTargetEffectResolver;

        // A Shader is a struct, so the loader's result is boxed into the resolver's object? return.
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path =>
            ShadowDuskShaderLoader.CanLoad(path)
                ? loader.Load(path)
                : previousResolver?.Invoke(path);
    }
}
