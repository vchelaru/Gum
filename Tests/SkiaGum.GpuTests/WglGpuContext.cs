using System.Runtime.InteropServices;
using SkiaSharp;

namespace SkiaGum.GpuTests;

/// <summary>
/// A GPU-backed Skia context (GRContext over an OpenGL context from a hidden window) for tests that need the
/// Ganesh sampling path. Windows only; on a machine without a GPU or on another OS it throws, so a test fails
/// loudly instead of passing without having tested anything.
/// </summary>
public sealed class WglGpuContext
{
    [StructLayout(LayoutKind.Sequential)]
    struct PixelFormatDescriptor
    {
        public ushort nSize, nVersion;
        public uint dwFlags;
        public byte iPixelType, cColorBits, cRedBits, cRedShift, cGreenBits, cGreenShift, cBlueBits, cBlueShift,
            cAlphaBits, cAlphaShift, cAccumBits, cAccumRedBits, cAccumGreenBits, cAccumBlueBits, cAccumAlphaBits,
            cDepthBits, cStencilBits, cAuxBuffers, iLayerType, bReserved;
        public uint dwLayerMask, dwVisibleMask, dwDamageMask;
    }

    [DllImport("user32", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateWindowExW(uint ex, string cls, string name, uint style, int x, int y, int w, int h,
        IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
    [DllImport("user32")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("gdi32")] static extern int ChoosePixelFormat(IntPtr dc, ref PixelFormatDescriptor pfd);
    [DllImport("gdi32")] static extern bool SetPixelFormat(IntPtr dc, int format, ref PixelFormatDescriptor pfd);
    [DllImport("opengl32")] static extern IntPtr wglCreateContext(IntPtr dc);
    [DllImport("opengl32")] static extern bool wglMakeCurrent(IntPtr dc, IntPtr rc);

    static readonly Lazy<WglGpuContext> LazyInstance = new(() => new WglGpuContext());
    // xunit runs tests on arbitrary threads; the GL context is current on one thread at a time.
    static readonly object Lock = new();

    public static WglGpuContext Instance => LazyInstance.Value;

    readonly IntPtr _dc;
    readonly IntPtr _rc;
    readonly GRContext _grContext;

    WglGpuContext()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("SkiaGum.GpuTests creates its GL context with WGL and runs on Windows only.");
        }
        // Style 0x80000000 is WS_POPUP and the window is never shown.
        IntPtr hwnd = CreateWindowExW(0, "STATIC", "SkiaGum.GpuTests", 0x80000000, 0, 0, 64, 64,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        _dc = GetDC(hwnd);
        var pfd = new PixelFormatDescriptor
        {
            nSize = (ushort)Marshal.SizeOf<PixelFormatDescriptor>(),
            nVersion = 1,
            dwFlags = 0x25, // PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER
            cColorBits = 32,
            cAlphaBits = 8,
            cDepthBits = 24,
            cStencilBits = 8,
        };
        SetPixelFormat(_dc, ChoosePixelFormat(_dc, ref pfd), ref pfd);
        _rc = wglCreateContext(_dc);
        MakeCurrent();
        _grContext = GRContext.CreateGl(GRGlInterface.Create())
            ?? throw new InvalidOperationException("GRContext.CreateGl failed; this machine has no usable OpenGL GPU.");
    }

    void MakeCurrent()
    {
        if (!wglMakeCurrent(_dc, _rc))
        {
            throw new InvalidOperationException("wglMakeCurrent failed.");
        }
    }

    /// <summary>
    /// Runs <paramref name="draw"/> against a fresh GPU surface of the given size and returns the pixels read back.
    /// </summary>
    public SKBitmap Render(int width, int height, int sampleCount, Action<SKCanvas> draw)
    {
        lock (Lock)
        {
            MakeCurrent();
            var info = new SKImageInfo(width, height);
            using SKSurface surface = SKSurface.Create(_grContext, true, info, sampleCount)
                ?? throw new InvalidOperationException("SKSurface.Create failed for the GPU context.");
            draw(surface.Canvas);
            surface.Canvas.Flush();
            var pixels = new SKBitmap(info);
            if (!surface.ReadPixels(info, pixels.GetPixels(), info.RowBytes, 0, 0))
            {
                throw new InvalidOperationException("ReadPixels failed.");
            }
            return pixels;
        }
    }
}
