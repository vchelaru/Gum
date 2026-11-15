using System.Runtime.InteropServices;

/// <summary>glFinish (so a benchmark frame includes the GPU finishing) and GL_RENDERER (which GPU produced the numbers).</summary>
sealed class GlHost
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void Finish();
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate IntPtr GetString(int name);

    private readonly Finish _finish;

    public GlHost(Func<string, IntPtr> getProc)
    {
        _finish = Marshal.GetDelegateForFunctionPointer<Finish>(getProc("glFinish"));
    }

    public void Sync() => _finish();

    public static string Renderer(Func<string, IntPtr> getProc) =>
        Marshal.PtrToStringAnsi(Marshal.GetDelegateForFunctionPointer<GetString>(getProc("glGetString"))(0x1F01)) ?? "unknown";
}
