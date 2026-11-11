#nullable enable

using Brutal.GlfwApi;
using SkiaGameRendering;
using SkiaSharp;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Gum;

/// <summary>
/// <see cref="IBrutalTarget"/> that draws straight into the window's default framebuffer through SkiaGameRendering.Brutal.OGL's
/// <see cref="SkiaDirectCanvas"/>: no intermediate texture and no blit. <see cref="Begin"/> does not clear, so Gum draws over
/// whatever the game already drew.
/// </summary>
internal sealed unsafe class GlTarget : IBrutalTarget
{
    private const int GL_FRAMEBUFFER = 0x8D40, GL_DRAW_FRAMEBUFFER_BINDING = 0x8CA6, GL_VIEWPORT = 0x0BA2;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void BindFramebufferFn(int target, int id);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ViewportFn(int x, int y, int width, int height);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void GetIntegervFn(int pname, int* values);

    private readonly SkiaDirectCanvas _direct;
    private readonly BindFramebufferFn _bindFramebuffer;
    private readonly ViewportFn _viewport;
    private readonly GetIntegervFn _getIntegerv;
    private int _gameFramebuffer;
    private int _gameX, _gameY, _gameWidth, _gameHeight;
    private readonly int _width;
    private readonly int _height;

    public GlTarget(SkiaDirectCanvas direct, Func<string, IntPtr> getProc, int width, int height)
    {
        _direct = direct;
        _bindFramebuffer = Marshal.GetDelegateForFunctionPointer<BindFramebufferFn>(getProc("glBindFramebuffer"));
        _viewport = Marshal.GetDelegateForFunctionPointer<ViewportFn>(getProc("glViewport"));
        _getIntegerv = Marshal.GetDelegateForFunctionPointer<GetIntegervFn>(getProc("glGetIntegerv"));
        _width = width;
        _height = height;
    }

    public SKCanvas Canvas => _direct.Canvas;

    public void Begin()
    {
        // The direct canvas takes its size from the viewport and its target from the bound draw framebuffer. End hands the game its own back.
        int framebuffer;
        int* viewport = stackalloc int[4];
        _getIntegerv(GL_DRAW_FRAMEBUFFER_BINDING, &framebuffer);
        _getIntegerv(GL_VIEWPORT, viewport);
        _gameFramebuffer = framebuffer;
        (_gameX, _gameY, _gameWidth, _gameHeight) = (viewport[0], viewport[1], viewport[2], viewport[3]);
        _bindFramebuffer(GL_FRAMEBUFFER, 0);
        _viewport(0, 0, _width, _height);
        _direct.Begin();
    }

    public void End()
    {
        _direct.End();
        RestoreGameState();
    }

    public void EndWithoutDrawing() => End();

    private void RestoreGameState()
    {
        _bindFramebuffer(GL_FRAMEBUFFER, _gameFramebuffer);
        _viewport(_gameX, _gameY, _gameWidth, _gameHeight);
    }

    // The shared SkiaDirectCanvas belongs to the GlBackend.
    public void Dispose() { }
}

/// <summary>Owns the SkiaGlContext for the window's current GL context and the SkiaDirectCanvas that draws into its default framebuffer.</summary>
internal sealed class GlBackend : IBrutalGpuBackend
{
    private readonly SkiaGlContext _context;
    private readonly SkiaDirectCanvas _direct;

    public GlBackend()
    {
        _context = new SkiaGlContext(GetProc);
        _direct = new SkiaDirectCanvas(_context);
    }

    // The window's GL context must be current on the calling thread, which Glfw.GetProcAddress also needs.
    private static IntPtr GetProc(string name) => Glfw.GetProcAddress(Encoding.ASCII.GetBytes(name + "\0"));

    public IBrutalTarget CreateTarget(int width, int height) => new GlTarget(_direct, GetProc, width, height);

    // Gum already drew into the default framebuffer; nothing to show.
    public void Present(IBrutalTarget target, int width, int height) { }

    // Everything is in GL command order; nothing to wait for.
    public void WaitForGpu() { }

    public void Dispose()
    {
        _direct.Dispose();
        _context.Dispose();
    }
}
