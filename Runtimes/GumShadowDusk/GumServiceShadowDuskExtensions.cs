using System;
using Microsoft.Xna.Framework.Graphics;
using ShadowDusk.Core;
using Gum.Wireframe;

namespace Gum;

/// <summary>
/// Adds <c>.fx</c> and <c>.slang</c> shader loading to a render-target container's
/// <c>SourceShaderFile</c>.
/// </summary>
public static class GumServiceShadowDuskExtensions
{
    /// <summary>
    /// Lets <c>SourceShaderFile</c> point at an <c>.fx</c> or <c>.slang</c> file, which ShadowDusk compiles
    /// at runtime into an <see cref="Effect"/> on the service's <see cref="GraphicsDevice"/>, for the graphics
    /// backend that device uses. Call once at startup. A shader ShadowDusk does not read keeps loading through whatever resolver
    /// was registered before this call.
    /// </summary>
    /// <param name="gumService">The service whose graphics device the effects are created on. It does not
    /// need to be initialized yet; the device is read when the first shader loads.</param>
    /// <returns><paramref name="gumService"/>, so calls chain.</returns>
    public static T UseShadowDusk<T>(this T gumService) where T : GumService
    {
        if (gumService == null)
        {
            throw new ArgumentNullException(nameof(gumService));
        }

        Func<ShaderSource, object?>? previousResolver = CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver;
        PlatformTarget? target = null;

        // ShadowDusk goes first and decides from the content whether it reads the shader; an input it does not
        // read (for example an .sksl or .glsl file) falls through to the resolver registered before this call.
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = source =>
        {
            // ShadowDusk cannot decide about bytes it cannot read; the previous resolver gets the source.
            string text;
            try
            {
                text = source.ReadAllText();
            }
            catch (System.IO.IOException) when (previousResolver != null)
            {
                return previousResolver(source);
            }

            if (!ShadowDuskEffectLoader.CanLoad(text, source.Name))
            {
                return previousResolver?.Invoke(source);
            }

            GraphicsDevice graphicsDevice = GetGraphicsDevice(gumService);
            target ??= ShadowDuskTargetDetector.Detect(graphicsDevice);
            return ShadowDuskEffectLoader.TryLoad(graphicsDevice, source.Name, text, target.Value)
                ?? previousResolver?.Invoke(source);
        };

        return gumService;
    }

    private static GraphicsDevice GetGraphicsDevice(GumService gumService)
    {
        // Read when a shader loads, so UseShadowDusk can be called before Initialize.
        return gumService.Renderer.GraphicsDevice
            ?? throw new InvalidOperationException(
                "A shader cannot load before the GumService is initialized, because its Effect needs the GraphicsDevice. Call GumService.Initialize first.");
    }
}
