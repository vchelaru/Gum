using System.Runtime.InteropServices;

namespace KniSkiaGumSample;

/// <summary>The host-specific benchmark bits on KNI DesktopGL (SDL2): glFinish (so a frame includes the GPU finishing),
/// GL_RENDERER (which GPU produced the numbers) and hiding the window. #8052</summary>
internal static class BenchmarkHost
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr SDL_GL_GetProcAddress(string proc);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_HideWindow(IntPtr window);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void FinishDelegate();
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate IntPtr GetStringDelegate(int name);

    private static FinishDelegate? _finish;

    public static void Finish()
    {
        _finish ??= Marshal.GetDelegateForFunctionPointer<FinishDelegate>(SDL_GL_GetProcAddress("glFinish"));
        _finish();
    }

    public static string Renderer() =>
        Marshal.PtrToStringAnsi(Marshal.GetDelegateForFunctionPointer<GetStringDelegate>(SDL_GL_GetProcAddress("glGetString"))(0x1F01)) ?? "unknown";

    public static void HideWindow(IntPtr handle) => SDL_HideWindow(handle);
}
