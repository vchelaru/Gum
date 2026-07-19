using System;
using Microsoft.Xna.Framework.Graphics;

namespace XnaAndWinforms;

/// <summary>
/// Adapts an <see cref="IGraphicsDeviceService"/> and a service provider to
/// <see cref="IRenderDeviceHost"/> via 1:1 forwarding, so render-initialization code doesn't need
/// to depend on the concrete <see cref="GraphicsDeviceControl"/> type.
/// </summary>
public class GraphicsDeviceServiceRenderHostAdapter : IRenderDeviceHost
{
    private readonly IGraphicsDeviceService _graphicsDeviceService;
    private readonly IServiceProvider _services;

    public GraphicsDeviceServiceRenderHostAdapter(IGraphicsDeviceService graphicsDeviceService, IServiceProvider services)
    {
        if (graphicsDeviceService == null)
        {
            throw new ArgumentNullException(nameof(graphicsDeviceService));
        }
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        _graphicsDeviceService = graphicsDeviceService;
        _services = services;
    }

    public GraphicsDevice GraphicsDevice => _graphicsDeviceService.GraphicsDevice;

    public IServiceProvider Services => _services;
}
