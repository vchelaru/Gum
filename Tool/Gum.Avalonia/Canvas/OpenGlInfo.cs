using System;
using System.Runtime.InteropServices;

namespace Gum.Avalonia.Canvas;

/// <summary>
/// The GL renderer string and GL_MAX_TEXTURE_SIZE of the context current on the calling thread, read
/// straight from the system GL library because KNI does not expose them. Diagnostic only: any failure
/// yields a short note instead of throwing.
/// </summary>
internal static class OpenGlInfo
{
    private const int GlRenderer = 0x1F01;
    private const int GlMaxTextureSize = 0x0D33;

    private delegate IntPtr GetStringFunction(int name);
    private delegate void GetIntegerFunction(int name, out int value);

    /// <summary>Returns text such as <c>renderer 'llvmpipe (LLVM 15.0.7, 256 bits)', max texture size 16384</c>.</summary>
    public static string Describe()
    {
        try
        {
            string? library = OperatingSystem.IsLinux() ? "libGL.so.1"
                : OperatingSystem.IsWindows() ? "opengl32"
                : OperatingSystem.IsMacOS() ? "/System/Library/Frameworks/OpenGL.framework/OpenGL"
                : null;
            if (library == null || !NativeLibrary.TryLoad(library, out IntPtr handle))
            {
                return "GL library not found";
            }
            GetStringFunction getString = Marshal.GetDelegateForFunctionPointer<GetStringFunction>(NativeLibrary.GetExport(handle, "glGetString"));
            GetIntegerFunction getInteger = Marshal.GetDelegateForFunctionPointer<GetIntegerFunction>(NativeLibrary.GetExport(handle, "glGetIntegerv"));
            string? renderer = Marshal.PtrToStringAnsi(getString(GlRenderer));
            getInteger(GlMaxTextureSize, out int maxTextureSize);
            return $"renderer '{renderer ?? "none (no current context)"}', max texture size {maxTextureSize}";
        }
        catch (Exception exception)
        {
            return "GL query failed: " + exception.Message;
        }
    }
}
