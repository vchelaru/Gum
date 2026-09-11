using System.Runtime.InteropServices;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Canvas;
using InputLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shouldly;
using XnaAndWinforms;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Avalonia canvas host: the shared device comes up without a window, the neutral frame
/// loop draws into its render target and reads it back, and the input adapter maps Avalonia
/// input to the polled contract. Device tests need a display and a GL driver, so they are
/// skipped on a headless runner.
/// </summary>
public class CanvasHostTests
{
    private static bool HasDisplay =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));

    private sealed class ClearingClient : IRenderTargetFrameClient
    {
        private readonly GraphicsDevice _device;
        public byte[]? Pixels;
        public int Width;
        public int Height;
        public string? LastError;

        public ClearingClient(GraphicsDevice device)
        {
            _device = device;
        }

        public void PreDrawUpdate() { }
        public void Draw() => _device.Clear(new Color(200, 30, 10, 255));
        public void Present(RenderTarget2D renderTarget)
        {
            Width = renderTarget.Width;
            Height = renderTarget.Height;
            Pixels = new byte[Width * Height * 4];
            renderTarget.GetData(Pixels);
        }
        public void ShowError(string? message) => LastError = message;
        public void ReportError(Exception exception) => LastError = exception.ToString();
    }

    [SkippableFact]
    public void FrameLoop_DrawsIntoTheSharedDevice_AndReadsBack()
    {
        Skip.IfNot(HasDisplay, "needs a display and a GL driver");

        using GameRenderDeviceHost host = new GameRenderDeviceHost();
        RenderTargetFrameLoop loop = new RenderTargetFrameLoop(host, new FrameRateThrottle()) { DesiredFramesPerSecond = 0 };
        ClearingClient client = new ClearingClient(host.GraphicsDevice);

        bool rendered = loop.TryRenderFrame(64, 48, client);

        rendered.ShouldBeTrue(client.LastError);
        client.Width.ShouldBe(64);
        client.Height.ShouldBe(48);
        client.Pixels.ShouldNotBeNull();
        // SurfaceFormat.Color reads back as RGBA.
        client.Pixels[0].ShouldBe((byte)200);
        client.Pixels[1].ShouldBe((byte)30);
        client.Pixels[2].ShouldBe((byte)10);
        client.Pixels[3].ShouldBe((byte)255);
    }

    [SkippableFact]
    public void TwoHosts_ShareOneDevice()
    {
        Skip.IfNot(HasDisplay, "needs a display and a GL driver");

        using GameRenderDeviceHost first = new GameRenderDeviceHost();
        using GameRenderDeviceHost second = new GameRenderDeviceHost();

        ReferenceEquals(first.GraphicsDevice, second.GraphicsDevice).ShouldBeTrue();
        first.Services.GetService(typeof(IGraphicsDeviceService)).ShouldNotBeNull();
    }

    [Fact]
    public void RenderSurface_CopiesRgbaIntoTheBitmap()
    {
        using AvaloniaRenderSurface surface = new AvaloniaRenderSurface();
        surface.Resize(2, 1);
        surface.RawImageBuffer[0] = 1;
        surface.RawImageBuffer[1] = 2;
        surface.RawImageBuffer[2] = 3;
        surface.RawImageBuffer[3] = 4;

        surface.Push(SurfaceFormat.Color);

        surface.Bitmap.ShouldNotBeNull();
        surface.Bitmap.PixelSize.Width.ShouldBe(2);
        Should.Throw<NotSupportedException>(() => surface.Push(SurfaceFormat.Bgra32));
    }

    [Theory]
    [InlineData(Key.A, XnaKeys.A)]
    [InlineData(Key.D1, XnaKeys.D1)]
    [InlineData(Key.Return, XnaKeys.Enter)]
    [InlineData(Key.LeftCtrl, XnaKeys.LeftControl)]
    [InlineData(Key.LeftShift, XnaKeys.LeftShift)]
    [InlineData(Key.Escape, XnaKeys.Escape)]
    [InlineData(Key.Delete, XnaKeys.Delete)]
    [InlineData(Key.OemPlus, XnaKeys.OemPlus)]
    [InlineData(Key.F5, XnaKeys.F5)]
    public void InputAdapter_MapsAvaloniaKeysToXnaKeys(Key avaloniaKey, XnaKeys expected)
    {
        AvaloniaInputHostAdapter.ToXnaKey(avaloniaKey).ShouldBe(expected);
    }

    [AvaloniaFact]
    public void InputAdapter_ReportsPointerAndCursorOnTheControl()
    {
        global::Avalonia.Controls.Border control = new global::Avalonia.Controls.Border { Width = 100, Height = 80 };
        AvaloniaInputHostAdapter adapter = new AvaloniaInputHostAdapter(control);

        adapter.GetPointerState().X.ShouldBe(-1);
        adapter.GetKeyboardState().GetPressedKeys().ShouldBeEmpty();

        adapter.Cursor = CursorKind.SizeNS;

        adapter.Cursor.ShouldBe(CursorKind.SizeNS);
        control.Cursor.ShouldNotBeNull();
    }
}
