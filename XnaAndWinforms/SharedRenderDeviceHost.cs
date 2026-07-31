using System;
using Microsoft.Xna.Framework.Graphics;

namespace XnaAndWinforms;

/// <inheritdoc/>
public class SharedRenderDeviceHost : ISharedRenderDeviceHost
{
    private const int PreferredMultiSampleCount = 1;
    private const DepthFormat RenderTargetDepthFormat = DepthFormat.Depth24Stencil8;

    private readonly IRenderDeviceResetPolicy _resetPolicy;
    private readonly ServiceContainer _services;
    private readonly SurfaceFormat _renderTargetFormat;

    private GraphicsDeviceService? _graphicsDeviceService;
    private RenderTarget2D? _renderTarget;

    /// <inheritdoc/>
    public GraphicsDevice GraphicsDevice => _graphicsDeviceService!.GraphicsDevice;

    /// <inheritdoc/>
    public IServiceProvider Services => _services;

    /// <inheritdoc/>
    public RenderTarget2D? RenderTarget => _renderTarget;

    /// <inheritdoc/>
    public event Action<int, int>? RenderTargetRecreated;

    /// <param name="windowHandle">
    /// The handle of the window the device presents against. WinForms clients pass their control
    /// handle; WPF clients pass the <c>HwndSource</c> handle of the window they are attached to.
    /// </param>
    /// <param name="width">The initial surface width in pixels.</param>
    /// <param name="height">The initial surface height in pixels.</param>
    /// <param name="renderTargetFormat">
    /// The render target's surface format. Defaults to <see cref="SurfaceFormat.Bgra32"/>, which
    /// blits to a GDI+ bitmap without a per-pixel channel swap.
    /// </param>
    public SharedRenderDeviceHost(IntPtr windowHandle, int width, int height,
        SurfaceFormat renderTargetFormat = SurfaceFormat.Bgra32)
        : this(windowHandle, width, height, new RenderDeviceResetPolicy(), renderTargetFormat)
    {
    }

    public SharedRenderDeviceHost(IntPtr windowHandle, int width, int height,
        IRenderDeviceResetPolicy resetPolicy, SurfaceFormat renderTargetFormat = SurfaceFormat.Bgra32)
    {
        _resetPolicy = resetPolicy;
        _renderTargetFormat = renderTargetFormat;
        _services = new ServiceContainer();

        _graphicsDeviceService = GraphicsDeviceService.AddRef(windowHandle, width, height);

        // Registered so components like ContentManager can find the device.
        _services.AddService<IGraphicsDeviceService>(_graphicsDeviceService);
    }

    /// <inheritdoc/>
    public void EnsureSurfaceSize(int surfaceWidth, int surfaceHeight, RenderingError error)
    {
        // Don't attempt to reset if we failed resetting before.
        if (error.GraphicsDeviceResetFailed)
        {
            return;
        }

        PresentationParameters presentationParameters = GraphicsDevice.PresentationParameters;
        RenderDeviceResetDecision decision = _resetPolicy.Evaluate(
            GraphicsDevice.GraphicsDeviceStatus,
            presentationParameters.BackBufferWidth,
            presentationParameters.BackBufferHeight,
            surfaceWidth,
            surfaceHeight);

        if (decision.Message != null)
        {
            error.Message = decision.Message;
        }
        if (decision.IsDeviceLost)
        {
            error.GraphicsDeviceLost = true;
        }
        if (decision.NeedsReset)
        {
            error.GraphicsDeviceNeedsReset = true;
        }

        if (error.GraphicsDeviceNeedsReset)
        {
            try
            {
                _graphicsDeviceService!.ResetDevice(surfaceWidth, surfaceHeight);

                error.Message = string.Empty;
                error.GraphicsDeviceNeedsReset = false;
            }
            catch (Exception e)
            {
                error.GraphicsDeviceResetFailed = true;
                error.GraphicsDeviceNeedsReset = false;
                error.Message = "Graphics device reset failed\n\n" + e;
            }
        }

        if (_renderTarget != null &&
            (decision.TargetWidth != _renderTarget.Width || decision.TargetHeight != _renderTarget.Height))
        {
            _renderTarget.Dispose();
            _renderTarget = null;
        }

        if (_renderTarget == null)
        {
            _renderTarget = new RenderTarget2D(
                GraphicsDevice, decision.TargetWidth, decision.TargetHeight,
                mipMap: false, _renderTargetFormat, RenderTargetDepthFormat, PreferredMultiSampleCount,
                // needed for rendering IsRenderTarget containers
                RenderTargetUsage.PreserveContents);

            RenderTargetRecreated?.Invoke(decision.TargetWidth, decision.TargetHeight);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_renderTarget != null)
        {
            _renderTarget.Dispose();
            _renderTarget = null;
        }

        if (_graphicsDeviceService != null)
        {
            _graphicsDeviceService.Release(disposing: true);
            _graphicsDeviceService = null;
        }
    }
}
