using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
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
/// skipped on a headless machine and on CI runners unless <c>GUM_RUN_CANVAS_DEVICE_TESTS=1</c>
/// opts in (the Linux CI job does, under Xvfb with Mesa's software GL). A GPU-less runner is not
/// merely slow: KNI's device creation fails there and the half-built device's finalizer crashes
/// the whole test host.
/// </summary>
public class CanvasHostTests
{
    private const string SkipReason = "needs a display and a GL driver; set GUM_RUN_CANVAS_DEVICE_TESTS=1 to run on CI";

    private static bool HasDisplay => TestEnvironment.CanUseDisplay("GUM_RUN_CANVAS_DEVICE_TESTS");

    // The shared KNI GL device belongs to the thread that created it, and the head creates it on
    // the Avalonia UI thread, so device tests run there too instead of on an xunit worker.
    private static void OnUiThread(Action action) =>
        HeadlessUnitTestSession.GetOrStartForAssembly(typeof(CanvasHostTests).Assembly)
            .Dispatch(action, CancellationToken.None).GetAwaiter().GetResult();

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
        Skip.IfNot(HasDisplay, SkipReason);

        OnUiThread(() =>
        {
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
        });
    }

    [SkippableFact]
    public void TwoHosts_ShareOneDevice()
    {
        Skip.IfNot(HasDisplay, SkipReason);

        OnUiThread(() =>
        {
            using GameRenderDeviceHost first = new GameRenderDeviceHost();
            using GameRenderDeviceHost second = new GameRenderDeviceHost();

            ReferenceEquals(first.GraphicsDevice, second.GraphicsDevice).ShouldBeTrue();
            first.Services.GetService(typeof(IGraphicsDeviceService)).ShouldNotBeNull();
        });
    }

    // Plugin StartUp builds the canvas control long before anything renders, and a machine
    // without GL cannot even attempt device creation safely (see the class remarks), so the
    // control must stay device-free until it first draws or is asked for the device.
    [AvaloniaFact]
    public void Control_DoesNotCreateTheDevice_UntilItIsUsed()
    {
        using AvaloniaGraphicsDeviceControl control = new AvaloniaGraphicsDeviceControl();

        GameRenderDeviceHost.IsSharedDeviceCreated.ShouldBeFalse();
    }

    // The bitmap needs the Avalonia platform, which the UI thread session provides.
    [AvaloniaFact]
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

    // #4811 (Avalonia parity with the WPF fix in #4681/#4682): the bitmap must always stay at 96
    // DPI regardless of display scale - Avalonia's compositor double-scales a Stretch.None-displayed
    // bitmap stamped at a non-96 DPI (github.com/AvaloniaUI/Avalonia/issues/17235), so the surface
    // must not vary its DPI stamp; physical-to-DIU scaling is instead the host control's job via
    // Stretch.Fill and explicit Width/Height (see AvaloniaGraphicsDeviceControl).
    [AvaloniaFact]
    public void RenderSurface_AlwaysStamps96Dpi_RegardlessOfSize()
    {
        using AvaloniaRenderSurface surface = new AvaloniaRenderSurface();

        surface.Resize(4, 3);

        surface.Bitmap.ShouldNotBeNull();
        surface.Bitmap.Dpi.X.ShouldBe(96);
        surface.Bitmap.Dpi.Y.ShouldBe(96);
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
    public void InputAdapter_ForgetsThePointerPosition_WhenThePointerLeavesTheControl()
    {
        // The canvas polls the position to decide whether the cursor is over it. A last in-bounds
        // position lingering after the pointer left read as "still over the canvas" and cleared the
        // tree's hover highlight every frame (#4694).
        global::Avalonia.Controls.Border control = new global::Avalonia.Controls.Border { Width = 100, Height = 80, Background = global::Avalonia.Media.Brushes.Red };
        global::Avalonia.Controls.Window window = new global::Avalonia.Controls.Window
        {
            Width = 300,
            Height = 300,
            Content = new global::Avalonia.Controls.Canvas { Children = { control } },
        };
        window.Show();
        window.UpdateLayout();
        AvaloniaInputHostAdapter adapter = new AvaloniaInputHostAdapter(control);

        window.MouseMove(new global::Avalonia.Point(10, 10));
        adapter.GetPointerState().X.ShouldBe(10);

        window.MouseMove(new global::Avalonia.Point(200, 200));
        adapter.GetPointerState().X.ShouldBe(-1);
        adapter.GetPointerState().Y.ShouldBe(-1);
        window.Close();
    }

    [AvaloniaFact]
    public void InputAdapter_TracksPointerPosition_DuringNativeDragOver()
    {
        // A native OS drag-and-drop (dragging a tree item or a file from Explorer onto the
        // canvas) does not raise PointerMoved on the control it's hovering. Without tracking
        // DragOver too, the position stays wherever the pointer was before the drag started,
        // so a dropped instance lands there instead of at the drop point (#4704).
        global::Avalonia.Controls.Border control = new global::Avalonia.Controls.Border { Width = 100, Height = 80, Background = global::Avalonia.Media.Brushes.Red };
        global::Avalonia.Controls.Window window = new global::Avalonia.Controls.Window
        {
            Width = 300,
            Height = 300,
            Content = new global::Avalonia.Controls.Canvas { Children = { control } },
        };
        DragDrop.SetAllowDrop(control, true);
        window.Show();
        window.UpdateLayout();
        AvaloniaInputHostAdapter adapter = new AvaloniaInputHostAdapter(control);
        DataTransfer data = new DataTransfer();

        window.DragDrop(new global::Avalonia.Point(40, 25), RawDragEventType.DragEnter, data, DragDropEffects.Copy);
        window.DragDrop(new global::Avalonia.Point(40, 25), RawDragEventType.DragOver, data, DragDropEffects.Copy);

        adapter.GetPointerState().X.ShouldBe(40);
        adapter.GetPointerState().Y.ShouldBe(25);
        window.Close();
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

    // #4811: the render target/surface are sized in physical pixels (DIU * RenderScaling); this
    // pins the pure conversion math. The live per-monitor scale can't be driven from a headless
    // test (Avalonia.Headless has no scaling knob), so the end-to-end wiring is a manual check.
    [Theory]
    [InlineData(800.0, 1.0, 800)]
    [InlineData(800.0, 2.0, 1600)]
    [InlineData(801.4, 1.0, 801)]
    [InlineData(0.0, 2.0, 1)]
    [InlineData(-5.0, 2.0, 1)]
    public void ToPhysicalPixelSize_ConvertsDiuSizeByDpiScale(double diuSize, double dpiScale, int expected)
    {
        AvaloniaGraphicsDeviceControl.ToPhysicalPixelSize(diuSize, dpiScale).ShouldBe(expected);
    }

    // #4811: IInputHostControl.Width/Height and pointer positions must also convert from Avalonia's
    // DIU to physical pixels, or hit-testing/dragging desyncs from the (now correctly-sized)
    // rendered content by the display's scale factor - same defect as the WPF fix's second half
    // (#4681's "dragging moving objects at 2x the cursor's speed" symptom).
    [Theory]
    [InlineData(320.0, 1.0, 320)]
    [InlineData(320.0, 2.0, 640)]
    [InlineData(160.6, 1.0, 161)]
    [InlineData(0.0, 2.0, 0)]
    [InlineData(-50.0, 2.0, -100)]
    public void ToPhysicalPixels_ConvertsDiuValueByDpiScale(double diuValue, double dpiScale, int expected)
    {
        AvaloniaInputHostAdapter.ToPhysicalPixels(diuValue, dpiScale).ShouldBe(expected);
    }
}
