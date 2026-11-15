using System.Runtime.InteropServices;
using SkiaSharp;

namespace SkiaGum.GpuTests;

/// <summary>
/// A GPU-backed Skia context for <see cref="SharedTextureGpuCacheTests"/>. On Windows it is the same hidden-window
/// WGL context every other test here uses (Mesa llvmpipe on CI). On macOS it is an offscreen CGL context so the
/// test also runs on a developer Mac. Any other OS throws, so the test fails loudly instead of passing untested.
/// </summary>
public sealed class SharedGpuContext : IDisposable
{
    const string OpenGlFramework = "/System/Library/Frameworks/OpenGL.framework/OpenGL";

    [DllImport(OpenGlFramework)] static extern int CGLChoosePixelFormat(int[] attribs, out IntPtr pixelFormat, out int count);
    [DllImport(OpenGlFramework)] static extern int CGLCreateContext(IntPtr pixelFormat, IntPtr share, out IntPtr context);
    [DllImport(OpenGlFramework)] static extern int CGLSetCurrentContext(IntPtr context);
    [DllImport(OpenGlFramework)] static extern void CGLDestroyContext(IntPtr context);
    [DllImport(OpenGlFramework)] static extern void CGLDestroyPixelFormat(IntPtr pixelFormat);

    // The GL context is current on one thread at a time and xunit moves tests between threads.
    static readonly object Lock = new();

    readonly WglGpuContext? _wgl;
    readonly IntPtr _cglContext;
    readonly IntPtr _cglPixelFormat;
    readonly GRContext? _cglGrContext;

    public SharedGpuContext()
    {
        if (OperatingSystem.IsWindows())
        {
            _wgl = new WglGpuContext();
            Renderer = _wgl.Renderer;
        }
        else if (OperatingSystem.IsMacOS())
        {
            // kCGLPFAAccelerated (73), kCGLPFAOpenGLProfile (99) = kCGLOGLPVersion_3_2_Core.
            int[] attributes = { 73, 99, 0x3200, 0 };
            if (CGLChoosePixelFormat(attributes, out _cglPixelFormat, out _) != 0 || _cglPixelFormat == IntPtr.Zero
                || CGLCreateContext(_cglPixelFormat, IntPtr.Zero, out _cglContext) != 0)
            {
                throw new InvalidOperationException("CGL could not create an OpenGL 3.2 context.");
            }
            CGLSetCurrentContext(_cglContext);
            _cglGrContext = GRContext.CreateGl(GRGlInterface.Create())
                ?? throw new InvalidOperationException("GRContext.CreateGl failed on the CGL context.");
            Renderer = "CGL";
        }
        else
        {
            throw new PlatformNotSupportedException("The GPU tests create their GL context with WGL (Windows) or CGL (macOS).");
        }
    }

    /// <summary>Which GL implementation the tests run on.</summary>
    public string Renderer { get; }

    /// <summary>Runs <paramref name="use"/> with a fresh GPU surface and the GRContext behind it.</summary>
    public void WithSurface(int width, int height, Action<SKSurface, GRContext> use)
    {
        if (_wgl != null)
        {
            _wgl.WithSurface(width, height, use);
            return;
        }
        lock (Lock)
        {
            CGLSetCurrentContext(_cglContext);
            using SKSurface surface = SKSurface.Create(_cglGrContext, true, new SKImageInfo(width, height), 0)
                ?? throw new InvalidOperationException("SKSurface.Create failed for the GPU context.");
            use(surface, _cglGrContext!);
        }
    }

    public void Dispose()
    {
        if (_wgl != null)
        {
            _wgl.Dispose();
            return;
        }
        lock (Lock)
        {
            CGLSetCurrentContext(_cglContext);
            _cglGrContext?.Dispose();
            CGLSetCurrentContext(IntPtr.Zero);
            CGLDestroyContext(_cglContext);
            CGLDestroyPixelFormat(_cglPixelFormat);
        }
    }
}

/// <summary>
/// Its own collection, not <see cref="GpuCollection"/>, because that one builds a <see cref="WglGpuContext"/>, which
/// fails on macOS. Parallelization is off so no other collection draws through Gum's static managers meanwhile.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class SharedGpuCollection : ICollectionFixture<SharedGpuContext>
{
    public const string Name = "SharedGpu";
}
