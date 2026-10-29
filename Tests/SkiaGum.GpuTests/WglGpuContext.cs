using System.Runtime.InteropServices;
using SkiaSharp;

namespace SkiaGum.GpuTests;

/// <summary>
/// A GPU-backed Skia context (GRContext over an OpenGL context from a hidden window) for tests that need the
/// Ganesh sampling path. Windows only; on a machine without a GPU or on another OS it throws, so a test fails
/// loudly instead of passing without having tested anything.
/// </summary>
public sealed class WglGpuContext : IDisposable
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
    // The same two calls through opengl32.dll. A replacement opengl32.dll (Mesa llvmpipe, which CI drops beside the
    // test assembly) only sees pixel formats chosen through these; the gdi32 versions go through the installed driver.
    [DllImport("opengl32")] static extern int wglChoosePixelFormat(IntPtr dc, ref PixelFormatDescriptor pfd);
    [DllImport("opengl32")] static extern bool wglSetPixelFormat(IntPtr dc, int format, ref PixelFormatDescriptor pfd);
    [DllImport("opengl32")] static extern IntPtr wglCreateContext(IntPtr dc);
    [DllImport("opengl32")] static extern bool wglMakeCurrent(IntPtr dc, IntPtr rc);
    [DllImport("opengl32")] static extern bool wglDeleteContext(IntPtr rc);
    [DllImport("user32")] static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("user32")] static extern bool DestroyWindow(IntPtr hwnd);

    // xunit runs tests on arbitrary threads; the GL context is current on one thread at a time.
    static readonly object Lock = new();

    readonly IntPtr _hwnd;
    readonly IntPtr _dc;
    readonly IntPtr _rc;
    readonly GRContext _grContext;

    public WglGpuContext()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("SkiaGum.GpuTests creates its GL context with WGL and runs on Windows only.");
        }
        // A real GPU driver takes the gdi32 pixel format calls; Mesa's replacement opengl32.dll needs its own wgl
        // ones. A window's pixel format can be set once, so each attempt gets a fresh window.
        bool created = TryCreate(useWglPixelFormat: false, out _hwnd, out _dc, out _rc, out _grContext)
            || TryCreate(useWglPixelFormat: true, out _hwnd, out _dc, out _rc, out _grContext);
        if (!created)
        {
            throw new InvalidOperationException("GRContext.CreateGl failed; this machine has no usable OpenGL GPU.");
        }
    }

    static bool TryCreate(bool useWglPixelFormat, out IntPtr hwnd, out IntPtr dc, out IntPtr rc, out GRContext grContext)
    {
        dc = IntPtr.Zero;
        rc = IntPtr.Zero;
        grContext = null!;
        // Style 0x80000000 is WS_POPUP and the window is never shown.
        hwnd = CreateWindowExW(0, "STATIC", "SkiaGum.GpuTests", 0x80000000, 0, 0, 64, 64,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (hwnd != IntPtr.Zero)
        {
            dc = GetDC(hwnd);
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
            bool formatSet = useWglPixelFormat
                ? wglSetPixelFormat(dc, wglChoosePixelFormat(dc, ref pfd), ref pfd)
                : SetPixelFormat(dc, ChoosePixelFormat(dc, ref pfd), ref pfd);
            if (formatSet)
            {
                rc = wglCreateContext(dc);
                if (rc != IntPtr.Zero && wglMakeCurrent(dc, rc))
                {
                    GRContext? created = null;
                    try
                    {
                        created = GRContext.CreateGl(GRGlInterface.Create());
                    }
                    catch (Exception)
                    {
                        // Treated the same as a null context: this attempt failed.
                    }
                    if (created != null)
                    {
                        grContext = created;
                        return true;
                    }
                }
            }
        }
        if (rc != IntPtr.Zero)
        {
            wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            wglDeleteContext(rc);
        }
        if (dc != IntPtr.Zero)
        {
            ReleaseDC(hwnd, dc);
        }
        if (hwnd != IntPtr.Zero)
        {
            DestroyWindow(hwnd);
        }
        return false;
    }

    public void Dispose()
    {
        lock (Lock)
        {
            MakeCurrent();
            _grContext.Dispose();
            wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            wglDeleteContext(_rc);
            ReleaseDC(_hwnd, _dc);
            DestroyWindow(_hwnd);
        }
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
