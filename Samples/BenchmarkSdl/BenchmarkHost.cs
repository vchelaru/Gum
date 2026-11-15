#nullable enable
using System.Runtime.InteropServices;

namespace GumSamples.Benchmark;

/// <summary>The host-specific benchmark bits on an SDL2 desktop host (KNI DesktopGL, MonoGame DesktopGL): glFinish (so a frame includes the GPU finishing),
/// GL_RENDERER (which GPU produced the numbers) and hiding the window. #8052</summary>
internal static class BenchmarkHost
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr SDL_GL_GetProcAddress(string proc);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_HideWindow(IntPtr window);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void FinishDelegate();
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate IntPtr GetStringDelegate(int name);

    private static FinishDelegate? _finish;

    // KNI ships SDL2 as SDL2.dll on Windows, libSDL2-2.0.so.0 on Linux and libSDL2-2.0.0.dylib on macOS,
    // so the plain name "SDL2" only resolves on Windows.
    static BenchmarkHost()
    {
        NativeLibrary.SetDllImportResolver(typeof(BenchmarkHost).Assembly, (name, assembly, path) =>
        {
            if (name == "SDL2")
            {
                foreach (string candidate in new[] { "SDL2", "libSDL2-2.0.so.0", "libSDL2-2.0.0.dylib" })
                {
                    if (NativeLibrary.TryLoad(candidate, assembly, path, out IntPtr handle))
                    {
                        return handle;
                    }
                }
            }
            return IntPtr.Zero;
        });
    }

    public static void Finish()
    {
        _finish ??= Marshal.GetDelegateForFunctionPointer<FinishDelegate>(SDL_GL_GetProcAddress("glFinish"));
        _finish();
    }

    public static string Renderer() =>
        Marshal.PtrToStringAnsi(Marshal.GetDelegateForFunctionPointer<GetStringDelegate>(SDL_GL_GetProcAddress("glGetString"))(0x1F01)) ?? "unknown";

    public static void HideWindow(IntPtr handle) => SDL_HideWindow(handle);
}
