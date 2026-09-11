using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace XnaAndWinforms;

/// <summary>
/// One frame of an editor canvas, independent of the UI framework that hosts it: throttles to the
/// requested rate, keeps the shared device's render target sized to the surface, binds and clears
/// it, runs the client's update and draw, and hands the target to the client to present. Errors
/// are caught and displayed through the client instead of tearing the canvas down; after an error
/// the loop drops to a slow retry rate.
/// </summary>
public sealed class RenderTargetFrameLoop
{
    private const float ErrorRetryFramesPerSecond = 0.5f;

    private readonly ISharedRenderDeviceHost _deviceHost;
    private readonly IFrameRateThrottle _frameRateThrottle;
    private readonly RenderingError _renderError;
    private readonly Stopwatch _frameClock;
    private double _lastFrameMilliseconds;
    private bool _isRenderingFrame;

    /// <summary>Creates the loop over the device host the canvas draws with.</summary>
    public RenderTargetFrameLoop(ISharedRenderDeviceHost deviceHost, IFrameRateThrottle frameRateThrottle)
    {
        _deviceHost = deviceHost;
        _frameRateThrottle = frameRateThrottle;
        _renderError = new RenderingError();
        _frameClock = Stopwatch.StartNew();
        _lastFrameMilliseconds = double.NegativeInfinity;
        DesiredFramesPerSecond = 30;
    }

    /// <summary>
    /// The frame rate the canvas aims for. The host's render pass fires at its own cadence; passes
    /// beyond this rate are skipped. Non-positive means every pass renders.
    /// </summary>
    public float DesiredFramesPerSecond { get; set; }

    /// <summary>The device this loop draws with.</summary>
    public GraphicsDevice GraphicsDevice => _deviceHost.GraphicsDevice;

    /// <summary>The error state of the device and the last frame.</summary>
    public RenderingError Error => _renderError;

    /// <summary>
    /// Renders one frame at the given surface size if the throttle allows and no frame is in
    /// progress. Returns true when a frame was drawn and presented.
    /// </summary>
    public bool TryRenderFrame(int width, int height, IRenderTargetFrameClient client)
    {
        if (_isRenderingFrame || width < 1 || height < 1)
        {
            return false;
        }

        double nowMilliseconds = _frameClock.Elapsed.TotalMilliseconds;
        if (!_frameRateThrottle.ShouldRenderFrame(nowMilliseconds - _lastFrameMilliseconds, DesiredFramesPerSecond))
        {
            return false;
        }
        _lastFrameMilliseconds = nowMilliseconds;

        _isRenderingFrame = true;
        try
        {
            return RenderFrame(width, height, client);
        }
        finally
        {
            _isRenderingFrame = false;
        }
    }

    private bool RenderFrame(int width, int height, IRenderTargetFrameClient client)
    {
        client.PreDrawUpdate();

        if (!_renderError.HasErrors)
        {
            BeginDraw(width, height);
        }

        if (_renderError.HasErrors)
        {
            client.ShowError(_renderError.ProcessedMessage);
            DesiredFramesPerSecond = ErrorRetryFramesPerSecond;
            return false;
        }

        try
        {
            client.Draw();
            EndDraw(client);
            client.ShowError(null);
            return true;
        }
        catch (Exception exception)
        {
            client.ReportError(exception);
            _renderError.Message = exception.ToString();
            return false;
        }
    }

    private void BeginDraw(int width, int height)
    {
        if (!_renderError.HasErrors || _renderError.GraphicsDeviceNeedsReset)
        {
            _deviceHost.EnsureSurfaceSize(width, height, _renderError);
        }

        if (!_renderError.HasErrors)
        {
            GraphicsDevice.SetRenderTarget(_deviceHost.RenderTarget);
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
            GraphicsDevice.Viewport = new Viewport
            {
                X = 0,
                Y = 0,
                Width = width,
                Height = height,
                MinDepth = 0,
                MaxDepth = 1,
            };
        }
    }

    private void EndDraw(IRenderTargetFrameClient client)
    {
        try
        {
            GraphicsDevice.SetRenderTarget(null);
            RenderTarget2D? renderTarget = _deviceHost.RenderTarget;
            if (renderTarget != null)
            {
                client.Present(renderTarget);
            }
        }
        catch
        {
            // The device can be lost mid-frame; the next BeginDraw handles the reset, so the
            // dropped frame is swallowed here.
        }
    }
}
