#nullable enable

using Brutal.GlfwApi;
using SkiaGameRendering;
using SkiaSharp;
using System;
using System.Text;

namespace Gum;

/// <summary><see cref="IBrutalTarget"/> over a SkiaGameRendering.Brutal.OGL render target and the blit that shows it.</summary>
internal sealed class GlTarget : IBrutalTarget
{
    private readonly SkiaRenderTarget2D _target;
    private readonly GlBlit _blit;

    public GlTarget(SkiaGlContext context, Func<string, IntPtr> getProc, int width, int height)
    {
        _target = new SkiaRenderTarget2D(context, width, height);
        _blit = new GlBlit(getProc, _target.GlTexture);
    }

    public SKCanvas Canvas => _target.Canvas;

    public void Begin() => _target.Begin();

    public void End() => _target.End();

    public void EndWithoutDrawing() => _target.End();

    public void BlitToDefaultFramebuffer(int width, int height) => _blit.ToDefaultFramebuffer(width, height, width, height);

    public void Dispose()
    {
        _blit.Dispose();
        _target.Dispose();
    }
}

/// <summary>Owns the SkiaGlContext for the window's current GL context and blits finished targets to its default framebuffer.</summary>
internal sealed class GlBackend : IBrutalGpuBackend
{
    private readonly SkiaGlContext _context;

    public GlBackend()
    {
        _context = new SkiaGlContext(GetProc);
    }

    // The window's GL context must be current on the calling thread, which Glfw.GetProcAddress also needs.
    private static IntPtr GetProc(string name) => Glfw.GetProcAddress(Encoding.ASCII.GetBytes(name + "\0"));

    public IBrutalTarget CreateTarget(int width, int height) => new GlTarget(_context, GetProc, width, height);

    public void Present(IBrutalTarget target, int width, int height) =>
        ((GlTarget)target).BlitToDefaultFramebuffer(width, height);

    // glBlitFramebuffer reads the texture synchronously from the driver's point of view; nothing to wait for.
    public void WaitForGpu() { }

    public void Dispose() => _context.Dispose();
}
