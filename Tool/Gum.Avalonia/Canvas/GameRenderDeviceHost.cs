using System;
using Microsoft.Xna.Framework.Graphics;
using XnaAndWinforms;

namespace Gum.Avalonia.Canvas;

/// <summary>
/// One canvas's share of the Avalonia head's graphics device (<see cref="HeadlessDeviceGame"/>),
/// reference-counted so every canvas draws on the same device and shares the runtime's texture
/// cache, plus a render target sized to whatever the canvas last asked for. The counterpart of
/// the WPF head's window-handle-based <c>SharedRenderDeviceHost</c>.
/// </summary>
public sealed class GameRenderDeviceHost : ISharedRenderDeviceHost
{
    private const DepthFormat RenderTargetDepthFormat = DepthFormat.Depth24Stencil8;

    private static readonly object _gate = new object();
    private static readonly RetryingSharedInstance<HeadlessDeviceGame> _shared = new();
    private static int _referenceCount;

    private HeadlessDeviceGame? _game;
    private RenderTarget2D? _renderTarget;

    /// <summary>Whether any host currently holds the shared device. For tests.</summary>
    internal static bool IsSharedDeviceCreated
    {
        get
        {
            lock (_gate)
            {
                return _referenceCount > 0;
            }
        }
    }

    /// <summary>Takes a reference on the shared device, creating it on first use.</summary>
    public GameRenderDeviceHost()
    {
        lock (_gate)
        {
            _game = _shared.GetOrCreate(() => new HeadlessDeviceGame(), g => g.EnsureInitialized());
            _referenceCount++;
        }
    }

    /// <inheritdoc/>
    public GraphicsDevice GraphicsDevice => _game!.GraphicsDevice;

    /// <inheritdoc/>
    public IServiceProvider Services => _game!.Services;

    /// <inheritdoc/>
    public RenderTarget2D? RenderTarget => _renderTarget;

    /// <inheritdoc/>
    public event Action<int, int>? RenderTargetRecreated;

    /// <inheritdoc/>
    public void EnsureSurfaceSize(int surfaceWidth, int surfaceHeight, RenderingError error)
    {
        if (GraphicsDevice.GraphicsDeviceStatus == GraphicsDeviceStatus.Lost)
        {
            error.GraphicsDeviceLost = true;
            error.Message = "Graphics device lost";
            return;
        }

        // A render target on this device is not bounded by the back buffer, so unlike the DX11
        // host there is nothing to reset: only the target itself follows the surface size.
        int width = Math.Max(1, surfaceWidth);
        int height = Math.Max(1, surfaceHeight);
        if (_renderTarget != null && (_renderTarget.Width != width || _renderTarget.Height != height))
        {
            _renderTarget.Dispose();
            _renderTarget = null;
        }

        if (_renderTarget == null)
        {
            _renderTarget = new RenderTarget2D(
                GraphicsDevice, width, height,
                mipMap: false, SurfaceFormat.Color, RenderTargetDepthFormat, preferredMultiSampleCount: 0,
                // needed for rendering IsRenderTarget containers
                RenderTargetUsage.PreserveContents);
            RenderTargetRecreated?.Invoke(width, height);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _renderTarget?.Dispose();
        _renderTarget = null;

        if (_game == null)
        {
            return;
        }
        HeadlessDeviceGame game = _game;
        _game = null;
        lock (_gate)
        {
            _referenceCount--;
            if (_referenceCount == 0)
            {
                game.Dispose();
                _shared.Clear();
            }
        }
    }
}
