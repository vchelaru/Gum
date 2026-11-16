using System.Runtime.InteropServices;

/// <summary>Test-only readback of the window's default framebuffer, so a run can prove a frame reached the window.</summary>
static unsafe class GlReadback
{
    const int GL_FRAMEBUFFER = 0x8D40;
    const int GL_RGBA = 0x1908;
    const int GL_UNSIGNED_BYTE = 0x1401;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void BindFramebuffer(int target, int id);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void ReadPixels(int x, int y, int w, int h, int format, int type, byte* data);

    /// <summary>
    /// RGBA bytes of the default framebuffer's current back buffer (call after Draw, before SwapBuffers), row 0 = top of the window.
    /// </summary>
    public static byte[] ReadDefaultFramebuffer(Func<string, IntPtr> getProc, int width, int height)
    {
        T Load<T>(string name) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(getProc(name));
        Load<BindFramebuffer>("glBindFramebuffer")(GL_FRAMEBUFFER, 0);
        byte[] bottomUp = new byte[width * height * 4];
        fixed (byte* p = bottomUp)
            Load<ReadPixels>("glReadPixels")(0, 0, width, height, GL_RGBA, GL_UNSIGNED_BYTE, p);

        // glReadPixels returns the bottom row first.
        byte[] topDown = new byte[bottomUp.Length];
        int stride = width * 4;
        for (int row = 0; row < height; row++)
            Array.Copy(bottomUp, (height - 1 - row) * stride, topDown, row * stride, stride);
        return topDown;
    }
}

/// <summary>
/// The game's own background: clears the window's default framebuffer before Gum draws over it. The GL entry
/// points are looked up once, so clearing each frame allocates nothing.
/// </summary>
sealed unsafe class GlClear
{
    const int GL_FRAMEBUFFER = 0x8D40;
    const int GL_COLOR_BUFFER_BIT = 0x4000;

    readonly delegate* unmanaged[Stdcall]<int, int, void> _bindFramebuffer;
    readonly delegate* unmanaged[Stdcall]<float, float, float, float, void> _clearColor;
    readonly delegate* unmanaged[Stdcall]<int, void> _clear;

    public GlClear(Func<string, IntPtr> getProc)
    {
        _bindFramebuffer = (delegate* unmanaged[Stdcall]<int, int, void>)getProc("glBindFramebuffer");
        _clearColor = (delegate* unmanaged[Stdcall]<float, float, float, float, void>)getProc("glClearColor");
        _clear = (delegate* unmanaged[Stdcall]<int, void>)getProc("glClear");
    }

    public void ToColor(float r, float g, float b)
    {
        _bindFramebuffer(GL_FRAMEBUFFER, 0);
        _clearColor(r, g, b, 1f);
        _clear(GL_COLOR_BUFFER_BIT);
    }
}
