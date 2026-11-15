using Gum;
using Gum.GueDeriving;
using Gum.Converters;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;
using Stride.Graphics;
using Stride.Input;
using Xunit;

// One Stride device per process (see StrideBackend), and Gum's services are singletons.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace StrideGum.GpuTests;

/// <summary>
/// Smoke test that Gum.Stride really draws on a GPU device: <see cref="GumService.InitializeGpu"/> builds
/// the SkiaGameRendering target through <c>StrideGpuTargetFactory</c> and <c>SkiaTargetHolder</c>, and
/// <see cref="GumService.DrawStrideFrame(Texture, GraphicsContext)"/> (the body of what
/// <c>GumSceneRenderer</c> runs every frame) draws a known element into a Stride render target that is
/// then read back from the GPU. The device is window-less (software WARP where there is no hardware
/// adapter), like SkiaGameRendering's own Stride tests. There is no CPU rendering path anywhere here.
/// <para>
/// Not exercised: registering <c>GumSceneRenderer</c> into a <c>GraphicsCompositor</c>, and a real
/// swap-chain back buffer, which need a running <c>Game</c> and a window. The registration is covered by
/// <c>GumSceneRendererRegistrationTests</c> in StrideGum.Tests.
/// </para>
/// <para>
/// The device is Stride's Gamma color space, where the readback is exactly the colors Gum drew; the
/// default Linear pipeline is SkiaGameRendering's concern and is covered by its own composite-color tests.
/// If no device can be created the tests fail loudly (the device constructor throws); they never skip.
/// </para>
/// </summary>
public sealed partial class StrideGumGpuSmokeTests
{
    const int Width = 64;
    const int Height = 48;

    static readonly SKColor Red = new(255, 0, 0, 255);
    static readonly SKColor Green = new(0, 255, 0, 255);

    [Fact]
    public void Device_RunsOnTheGraphicsApiThisProjectTests()
    {
        GraphicsDevice device = StrideBackend.Device;
        GraphicsDevice.Platform.ShouldBe(StrideBackend.ExpectedPlatform);
    }

    [Fact]
    public void DrawStrideFrame_ColoredRectangle_RendersOpaqueInsideAndTransparentOutside()
    {
        GraphicsDevice device = StrideBackend.Device;
        GumService.Default.InitializeGpu(device, Width, Height, new InputManager(), null);
        AddRectangle(x: 10, y: 8, width: 20, height: 16, Red);

        using Texture target = NewTarget(device, Width, Height);
        DrawFrame(device, target);
        Pixels pixels = Read(device, target);

        pixels.At(20, 16).ShouldBe(Rgba(255, 0, 0, 255));
        pixels.At(50, 40).ShouldBe(Rgba(0, 0, 0, 0));
        pixels.At(5, 5).ShouldBe(Rgba(0, 0, 0, 0));
    }

    [Fact]
    public void DrawStrideFrame_AfterTargetResize_RecreatesTheCanvasAndReflowsLayout()
    {
        GraphicsDevice device = StrideBackend.Device;
        GumService.Default.InitializeGpu(device, Width, Height, new InputManager(), null);
        AddRectangle(x: 10, y: 8, width: 20, height: 16, Red);
        GraphicalUiElement corner = AddRectangle(x: 0, y: 0, width: 10, height: 10, Green);
        corner.XOrigin = HorizontalAlignment.Right;
        corner.XUnits = GeneralUnitType.PixelsFromLarge;
        corner.YOrigin = VerticalAlignment.Bottom;
        corner.YUnits = GeneralUnitType.PixelsFromLarge;

        using (Texture small = NewTarget(device, Width, Height))
        {
            DrawFrame(device, small);
            Pixels before = Read(device, small);
            before.At(20, 16).ShouldBe(Rgba(255, 0, 0, 255));
            before.At(Width - 5, Height - 5).ShouldBe(Rgba(0, 255, 0, 255));
        }

        // A bigger back buffer: the holder recreates the GPU target and tells Gum about the new size.
        const int bigWidth = 96;
        const int bigHeight = 72;
        using Texture big = NewTarget(device, bigWidth, bigHeight);
        DrawFrame(device, big);
        Pixels after = Read(device, big);

        after.At(20, 16).ShouldBe(Rgba(255, 0, 0, 255));
        after.At(bigWidth - 5, bigHeight - 5).ShouldBe(Rgba(0, 255, 0, 255));
        // Where the corner rectangle sat at the old size is empty now.
        after.At(Width - 5, Height - 5).ShouldBe(Rgba(0, 0, 0, 0));
        after.At(50, 40).ShouldBe(Rgba(0, 0, 0, 0));
    }

    static GraphicalUiElement AddRectangle(float x, float y, float width, float height, SKColor color)
    {
        RectangleRuntime rectangle = new()
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            IsFilled = true,
            FillColor = color,
        };
        GumService.Default.Root.AddChild(rectangle);
        return rectangle;
    }

    static Texture NewTarget(GraphicsDevice device, int width, int height) =>
        Texture.New2D(device, width, height, PixelFormat.R8G8B8A8_UNorm,
            TextureFlags.RenderTarget | TextureFlags.ShaderResource);

    // Binds and clears the target as a compositor would, then runs the same code GumSceneRenderer runs.
    static void DrawFrame(GraphicsDevice device, Texture target) =>
        StrideBackend.Frame(device, target, graphicsContext =>
            GumService.Default.DrawStrideFrame(target, graphicsContext));

    static Pixels Read(GraphicsDevice device, Texture target) =>
        new(StrideBackend.ReadRgba(device, target), target.Width);

    static (byte R, byte G, byte B, byte A) Rgba(byte r, byte g, byte b, byte a) => (r, g, b, a);

    sealed class Pixels
    {
        readonly byte[] _rgba;
        readonly int _width;

        internal Pixels(byte[] rgba, int width)
        {
            _rgba = rgba;
            _width = width;
        }

        internal (byte R, byte G, byte B, byte A) At(int x, int y)
        {
            int i = (y * _width + x) * 4;
            return (_rgba[i], _rgba[i + 1], _rgba[i + 2], _rgba[i + 3]);
        }
    }
}
