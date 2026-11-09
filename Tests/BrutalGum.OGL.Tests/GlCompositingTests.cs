using Brutal.GlfwApi;
using Brutal.Numerics;
using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaHostTests;
using SkiaSharp;
using System.Runtime.InteropServices;
using System.Text;

namespace BrutalGum.Tests;

/// <summary>
/// Gum composites over whatever the game already drew: a real hidden OpenGL window, the game clears the default
/// framebuffer, then Gum draws. Needs a GL 3.3 core context; without one the tests return without asserting.
/// </summary>
public unsafe class GlCompositingTests : BaseTestClass
{
    private const int GL_COLOR_BUFFER_BIT = 0x4000, GL_FRAMEBUFFER = 0x8D40, GL_RGBA = 0x1908, GL_UNSIGNED_BYTE = 0x1401;
    private const int Size = 64;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ClearColorFn(float r, float g, float b, float a);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ClearFn(int mask);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void BindFramebufferFn(int target, int id);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ReadPixelsFn(int x, int y, int w, int h, int format, int type, byte* data);

    private static IntPtr GetProc(string name) => Glfw.GetProcAddress(Encoding.ASCII.GetBytes(name + "\0"));

    private static T Load<T>(string name) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(GetProc(name));

    /// <summary>Runs the body with Gum initialized on a hidden GL window, or returns false when no GL context could be made.</summary>
    private static bool WithWindow(Action<GlfwWindow> body)
    {
        GlfwWindow? window = null;
        try
        {
            if (!Glfw.Init())
            {
                return false;
            }
            Glfw.WindowHint(GlfwWindowHint.ContextVersionMajor, 3);
            Glfw.WindowHint(GlfwWindowHint.ContextVersionMinor, 3);
            Glfw.WindowHint(GlfwWindowHint.OpenGlProfile, 0x32001);
            Glfw.WindowHint(GlfwWindowHint.Visible, Glfw.Constants.GLFW_FALSE);
            window = Glfw.CreateWindow(new GlfwWindow.CreateInfo { Title = "GlCompositingTests", Size = new int2(Size, Size) });
            window.MakeContextCurrent();
        }
        catch (Exception)
        {
            window?.Dispose();
            return false;
        }

        GumService.Default.Uninitialize();
        try
        {
            GumService.Default.Initialize(window);
            body(window);
        }
        finally
        {
            GumService.Default.Uninitialize();
            window.Dispose();
            SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
            GumService.Default.Initialize(surface.Canvas, TestAssemblyInitialize.Input, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
            SkiaHostTestBase.CaptureRenderableBaseline();
            Gum.Forms.Controls.FrameworkElement.KeyboardsForUiControl.Clear();
        }
        return true;
    }

    private static void GameClear(float r, float g, float b)
    {
        Load<BindFramebufferFn>("glBindFramebuffer")(GL_FRAMEBUFFER, 0);
        Load<ClearColorFn>("glClearColor")(r, g, b, 1f);
        Load<ClearFn>("glClear")(GL_COLOR_BUFFER_BIT);
    }

    private static (byte R, byte G, byte B, byte A) PixelAt(int x, int y)
    {
        Load<BindFramebufferFn>("glBindFramebuffer")(GL_FRAMEBUFFER, 0);
        byte[] pixel = new byte[4];
        fixed (byte* p = pixel)
        {
            Load<ReadPixelsFn>("glReadPixels")(x, y, 1, 1, GL_RGBA, GL_UNSIGNED_BYTE, p);
        }
        return (pixel[0], pixel[1], pixel[2], pixel[3]);
    }

    [Fact]
    public void Draw_WithAnEmptyScene_KeepsWhatTheGameDrew()
    {
        bool ran = WithWindow(window =>
        {
            GameClear(0f, 0f, 1f);

            GumService.Default.Update(0.016);
            GumService.Default.Draw();

            PixelAt(10, 10).ShouldBe(((byte)0, (byte)0, (byte)255, (byte)255));
        });
        _ = ran;
    }

    [Fact]
    public void Draw_WithAHalfTransparentElement_BlendsOverWhatTheGameDrew()
    {
        bool ran = WithWindow(window =>
        {
            GameClear(0f, 0f, 1f);
            ColoredRectangleRuntime rectangle = new() { X = 0, Y = 0, Width = Size, Height = Size, Color = new SKColor(255, 0, 0, 128) };
            GumService.Default.Root.AddChild(rectangle);

            GumService.Default.Update(0.016);
            GumService.Default.Draw();

            (byte r, byte g, byte b, byte a) = PixelAt(10, 10);
            // Red at alpha 128 over blue: about half of each.
            r.ShouldBeInRange((byte)120, (byte)136);
            g.ShouldBe((byte)0);
            b.ShouldBeInRange((byte)119, (byte)135);
            a.ShouldBe((byte)255);
        });
        _ = ran;
    }

    [Fact]
    public void Draw_AnElementAtTheTopOfTheCanvas_AppearsAtTheTopOfTheWindow()
    {
        bool ran = WithWindow(window =>
        {
            GameClear(0f, 0f, 1f);
            ColoredRectangleRuntime rectangle = new() { X = 0, Y = 0, Width = Size, Height = Size / 2, Color = SKColors.Red };
            GumService.Default.Root.AddChild(rectangle);

            GumService.Default.Update(0.016);
            GumService.Default.Draw();

            // glReadPixels rows count from the bottom of the window.
            PixelAt(10, Size - 10).ShouldBe(((byte)255, (byte)0, (byte)0, (byte)255));
            PixelAt(10, 10).ShouldBe(((byte)0, (byte)0, (byte)255, (byte)255));
        });
        _ = ran;
    }
}
