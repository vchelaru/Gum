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

    public SKCanvas Canvas => FlushProbe.NoDraw ? (_noDraw ??= new SKNoDrawCanvas(_width, _height)) : _ownActive ? _ownSurface!.Canvas : _direct.Canvas;
    private SKNoDrawCanvas? _noDraw; // SPIKE #8116: GUM_NODRAW=1 sends every Gum draw to a no-op canvas (walk-only cost)

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
        FlushProbe.BeginStart = System.Diagnostics.Stopwatch.GetTimestamp();
        if (FlushProbe.OwnPass && _ownSurface != null)
        {
            // SPIKE: SGR's Begin minus the GL queries, with a chosen reset mask.
            if (FlushProbe.Reset != "none") Gr.ResetContext();
            _ownSave = _ownSurface.Canvas.Save();
            _ownActive = true;
        }
        else _direct.Begin();
        FlushProbe.BeginEnd = System.Diagnostics.Stopwatch.GetTimestamp();
    }

    private static GRContext? _gr;
    private GRContext Gr => _gr ??= (GRContext)typeof(SkiaGlContext).GetField("_grContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
        .GetValue(typeof(SkiaDirectCanvas).GetField("_context", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(_direct))!;
    private SKSurface? _ownSurface;
    private int _ownSave;
    private bool _ownActive;

    public void End()
    {
        if (FlushProbe.Raw > 0) { FlushProbe.GlTargetGr = Gr; FlushProbe.DrawRaw(Canvas); }
        FlushProbe.FlushStart = System.Diagnostics.Stopwatch.GetTimestamp();
        if (FlushProbe.Enabled)
        {
            if (_ownActive) _ownSurface!.Canvas.RestoreToCount(_ownSave);
            else _direct.Canvas.RestoreToCount(1);
            Gr.Flush(false);
            FlushProbe.FlushEnd = System.Diagnostics.Stopwatch.GetTimestamp();
            if (!FlushProbe.Defer) Gr.Submit(false);
        }
        else FlushProbe.FlushEnd = System.Diagnostics.Stopwatch.GetTimestamp();
        FlushProbe.SubmitEnd = System.Diagnostics.Stopwatch.GetTimestamp();
        if (_ownActive)
        {
            _ownActive = false;
            if (!FlushProbe.Enabled) { _ownSurface!.Canvas.RestoreToCount(_ownSave); _ownSurface.Flush(); }
        }
        else
        {
            _direct.End();
            if (FlushProbe.OwnPass && _ownSurface == null)
                _ownSurface = (SKSurface?)typeof(SkiaDirectCanvas).GetField("_backBufferSurface", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(_direct);
        }
        RestoreGameState();
        FlushProbe.EndEnd = System.Diagnostics.Stopwatch.GetTimestamp();
        if (FlushProbe.Enabled) FlushProbe.FrameDone();
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
        if (FlushProbe.GlTime || FlushProbe.CtxOpt != null)
        {
            // SPIKE (#8098): rebuild the GRContext with timing wrappers and/or options variants.
            var field = typeof(SkiaGlContext).GetField("_grContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            ((GRContext)field.GetValue(_context)!).Dispose();
            var glInterface = GRGlInterface.Create(name => FlushProbe.Wrap(name, GetProc(name)))!;
            var options = new GRContextOptions();
            switch (FlushProbe.CtxOpt)
            {
                case "map0": options.BufferMapThreshold = 0; break;
                case "nomap": options.BufferMapThreshold = int.MaxValue; break;
                case "nostencil": options.AvoidStencilBuffers = true; break;
                case "atlas": options.GlyphCacheTextureMaximumBytes = 64 * 1024 * 1024; break;
                // SPIKE #8116 flush agent: knobs not tried before
                case "pathmask": options.AllowPathMaskCaching = true; break;
                case "mip": options.DoManualMipmapping = true; break;
                case "prog": options.RuntimeProgramCacheSize = 4096; break;
                case "smallatlas": options.GlyphCacheTextureMaximumBytes = 512 * 1024; break;
            }
            var gr = GRContext.CreateGl(glInterface, options);
            if (FlushProbe.CtxOpt == "rescache") gr.SetResourceCacheLimit(1L << 30);
            field.SetValue(_context, gr);
        }
        _direct = new SkiaDirectCanvas(_context);
    }

    // The window's GL context must be current on the calling thread, which Glfw.GetProcAddress also needs.
    private static IntPtr GetProc(string name) => Glfw.GetProcAddress(Encoding.ASCII.GetBytes(name + "\0"));

    public IBrutalTarget CreateTarget(int width, int height) => new GlTarget(_direct, GetProc, width, height);

    // Gum already drew into the default framebuffer; nothing to show.
    public void Present(IBrutalTarget target, int width, int height) { }

    // Everything is in GL command order; nothing to wait for.
    public void BeginFrame() { }

    public void WaitForGpu() { }

    public void Dispose()
    {
        _direct.Dispose();
        _context.Dispose();
    }
}
