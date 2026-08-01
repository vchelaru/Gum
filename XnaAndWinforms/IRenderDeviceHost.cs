using System;
using Microsoft.Xna.Framework.Graphics;

namespace XnaAndWinforms;

/// <summary>
/// The render-side subset of a rendering host that <c>SystemManagers.Initialize</c> and its
/// callers need: a GPU device to register against, and a service provider for constructing
/// <c>ContentManager</c>s. Lets that initialization run against any host - a live
/// <see cref="WpfGraphicsDeviceControl"/> or a test double - rather than one concrete control type.
/// </summary>
public interface IRenderDeviceHost
{
    /// <summary>
    /// The GPU device to initialize rendering against.
    /// </summary>
    GraphicsDevice GraphicsDevice { get; }

    /// <summary>
    /// The service provider used to construct <c>ContentManager</c>s for loading fonts/textures.
    /// </summary>
    IServiceProvider Services { get; }
}
