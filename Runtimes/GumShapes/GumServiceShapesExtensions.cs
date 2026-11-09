using System;
using Gum.Renderables;

namespace Gum;

/// <summary>
/// Enables the shape runtimes (<c>CircleRuntime</c>, <c>RoundedRectangleRuntime</c>, <c>ArcRuntime</c>,
/// <c>SvgRuntime</c> and so on) of this add-on package.
/// </summary>
public static class GumServiceShapesExtensions
{
    /// <summary>
    /// Initializes the shape renderer on this service's graphics device. Call once, after
    /// <c>Initialize</c>. Calling it again, or after <see cref="ShapeRenderer.Initialize()"/>, does nothing.
    /// Replaces <c>ShapeRenderer.Self.Initialize()</c>.
    /// </summary>
    /// <returns><paramref name="gumService"/>, so calls chain.</returns>
    /// <exception cref="InvalidOperationException">The service has not been initialized, so it has no graphics device.</exception>
    public static T UseShapes<T>(this T gumService) where T : GumService
    {
        if (gumService == null)
        {
            throw new ArgumentNullException(nameof(gumService));
        }

        if (!gumService.IsInitialized)
        {
            throw new InvalidOperationException(
                "UseShapes was called before the GumService was initialized, so it has no graphics device. Call Initialize first.");
        }

        if (!ShapeRenderer.Self.IsInitialized)
        {
            ShapeRenderer.Self.InitializeFor(gumService.Renderer.GraphicsDevice);
        }

        return gumService;
    }
}
