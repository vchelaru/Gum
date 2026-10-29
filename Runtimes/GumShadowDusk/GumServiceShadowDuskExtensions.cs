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
    /// backend that device uses. Call once at startup. Other extensions keep loading through whatever resolver
    /// was registered before this call.
    /// </summary>
    /// <param name="gumService">The service whose graphics device the effects are created on. It does not
    /// need to be initialized yet; the device is read when the first shader loads.</param>
    public static void UseShadowDusk(this GumService gumService)
    {
        if (gumService == null)
        {
            throw new ArgumentNullException(nameof(gumService));
        }

        Func<string, object?>? previousResolver = CustomSetPropertyOnRenderable.RenderTargetEffectResolver;
        PlatformTarget? target = null;

        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path =>
        {
            if (!ShadowDuskEffectLoader.CanLoad(path))
            {
                return previousResolver?.Invoke(path);
            }

            GraphicsDevice graphicsDevice = GetGraphicsDevice(gumService);
            target ??= ShadowDuskTargetDetector.Detect(graphicsDevice);
            return ShadowDuskEffectLoader.Load(graphicsDevice, path, target.Value);
        };
    }

    private static GraphicsDevice GetGraphicsDevice(GumService gumService)
    {
        // Read when a shader loads, so UseShadowDusk can be called before Initialize.
        return gumService.Renderer.GraphicsDevice
            ?? throw new InvalidOperationException(
                "A shader cannot load before the GumService is initialized, because its Effect needs the GraphicsDevice. Call GumService.Initialize first.");
    }
}
