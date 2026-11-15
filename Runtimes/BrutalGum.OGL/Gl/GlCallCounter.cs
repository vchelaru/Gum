using System;
using System.Runtime.InteropServices;

namespace Gum;

/// <summary>SPIKE: counts the GL calls Skia makes (draws, program changes, texture binds) when GUM_GLCOUNT=1. Wraps the proc addresses Skia loads.</summary>
public static unsafe class GlCallCounter
{
    public static readonly bool Enabled = Environment.GetEnvironmentVariable("GUM_GLCOUNT") == "1";
    public static long Draws;
    public static long Programs;
    public static long Textures;

    static IntPtr _drawArrays, _drawElements, _drawArraysInstanced, _drawElementsInstanced, _drawRangeElements, _useProgram, _bindTexture;

    public static IntPtr Wrap(string name, IntPtr real)
    {
        if (!Enabled || real == IntPtr.Zero) return real;
        switch (name)
        {
            case "glDrawArrays": _drawArrays = real; return (IntPtr)(delegate* unmanaged<uint, int, int, void>)&DrawArrays;
            case "glDrawElements": _drawElements = real; return (IntPtr)(delegate* unmanaged<uint, int, uint, IntPtr, void>)&DrawElements;
            case "glDrawArraysInstanced": _drawArraysInstanced = real; return (IntPtr)(delegate* unmanaged<uint, int, int, int, void>)&DrawArraysInstanced;
            case "glDrawElementsInstanced": _drawElementsInstanced = real; return (IntPtr)(delegate* unmanaged<uint, int, uint, IntPtr, int, void>)&DrawElementsInstanced;
            case "glDrawRangeElements": _drawRangeElements = real; return (IntPtr)(delegate* unmanaged<uint, uint, uint, int, uint, IntPtr, void>)&DrawRangeElements;
            case "glUseProgram": _useProgram = real; return (IntPtr)(delegate* unmanaged<uint, void>)&UseProgram;
            case "glBindTexture": _bindTexture = real; return (IntPtr)(delegate* unmanaged<uint, uint, void>)&BindTexture;
            default: return real;
        }
    }

    [UnmanagedCallersOnly] static void DrawArrays(uint a, int b, int c) { Draws++; ((delegate* unmanaged<uint, int, int, void>)_drawArrays)(a, b, c); }
    [UnmanagedCallersOnly] static void DrawElements(uint a, int b, uint c, IntPtr d) { Draws++; ((delegate* unmanaged<uint, int, uint, IntPtr, void>)_drawElements)(a, b, c, d); }
    [UnmanagedCallersOnly] static void DrawArraysInstanced(uint a, int b, int c, int d) { Draws++; ((delegate* unmanaged<uint, int, int, int, void>)_drawArraysInstanced)(a, b, c, d); }
    [UnmanagedCallersOnly] static void DrawElementsInstanced(uint a, int b, uint c, IntPtr d, int e) { Draws++; ((delegate* unmanaged<uint, int, uint, IntPtr, int, void>)_drawElementsInstanced)(a, b, c, d, e); }
    [UnmanagedCallersOnly] static void DrawRangeElements(uint a, uint b, uint c, int d, uint e, IntPtr f) { Draws++; ((delegate* unmanaged<uint, uint, uint, int, uint, IntPtr, void>)_drawRangeElements)(a, b, c, d, e, f); }
    [UnmanagedCallersOnly] static void UseProgram(uint a) { Programs++; ((delegate* unmanaged<uint, void>)_useProgram)(a); }
    [UnmanagedCallersOnly] static void BindTexture(uint a, uint b) { Textures++; ((delegate* unmanaged<uint, uint, void>)_bindTexture)(a, b); }
}
