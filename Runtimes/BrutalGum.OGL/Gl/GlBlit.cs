using System;
using System.Runtime.InteropServices;

namespace Gum;

/// <summary>Copies a GL texture to the default framebuffer with glBlitFramebuffer, flipped to the window's top-down orientation.</summary>
internal sealed class GlBlit : IDisposable
{
    private const int GL_READ_FRAMEBUFFER = 0x8CA8, GL_DRAW_FRAMEBUFFER = 0x8CA9, GL_COLOR_ATTACHMENT0 = 0x8CE0;
    private const int GL_TEXTURE_2D = 0x0DE1, GL_COLOR_BUFFER_BIT = 0x4000, GL_LINEAR = 0x2601;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void GenFramebuffers(int n, out int ids);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void DeleteFramebuffers(int n, ref int ids);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void BindFramebuffer(int target, int id);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void FramebufferTexture2D(int target, int attachment, int texTarget, int texture, int level);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void BlitFramebuffer(int sx0, int sy0, int sx1, int sy1, int dx0, int dy0, int dx1, int dy1, int mask, int filter);

    private readonly BindFramebuffer _bind;
    private readonly BlitFramebuffer _blit;
    private readonly DeleteFramebuffers _delete;
    private int _fbo;

    public GlBlit(Func<string, IntPtr> getProc, int texture)
    {
        T Load<T>(string name) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(getProc(name));
        _bind = Load<BindFramebuffer>("glBindFramebuffer");
        _blit = Load<BlitFramebuffer>("glBlitFramebuffer");
        _delete = Load<DeleteFramebuffers>("glDeleteFramebuffers");
        Load<GenFramebuffers>("glGenFramebuffers")(1, out _fbo);
        _bind(GL_READ_FRAMEBUFFER, _fbo);
        Load<FramebufferTexture2D>("glFramebufferTexture2D")(GL_READ_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, texture, 0);
        _bind(GL_READ_FRAMEBUFFER, 0);
    }

    /// <summary>Blits the texture (<paramref name="width"/> by <paramref name="height"/>) to the default framebuffer, filling it.</summary>
    public void ToDefaultFramebuffer(int width, int height, int framebufferWidth, int framebufferHeight)
    {
        (int x0, int y0, int x1, int y1) = GlBlitMath.FlippedDestination(framebufferWidth, framebufferHeight);
        _bind(GL_READ_FRAMEBUFFER, _fbo);
        _bind(GL_DRAW_FRAMEBUFFER, 0);
        _blit(0, 0, width, height, x0, y0, x1, y1, GL_COLOR_BUFFER_BIT, GL_LINEAR);
        _bind(GL_READ_FRAMEBUFFER, 0);
    }

    public void Dispose()
    {
        if (_fbo != 0)
        {
            _delete(1, ref _fbo);
            _fbo = 0;
        }
    }
}
