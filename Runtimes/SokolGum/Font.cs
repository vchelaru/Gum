using System.Runtime.InteropServices;
using static Sokol.Fontstash;

namespace SokolGum;

/// <summary>
/// A font loaded into the shared fontstash context. Holds the font id
/// returned by fontstash plus the unmanaged TTF byte buffer that fontstash
/// keeps a reference to internally (so we must not free it until the font
/// is disposed).
///
/// NOTE: fonsAddFontMem with freeData=0 keeps the caller's pointer alive for
/// the font's lifetime, so we allocate an unmanaged copy we own explicitly.
/// Handing fontstash a pinned managed byte[] would be unsafe — the GC can
/// relocate the buffer the moment we un-fix it.
/// </summary>
public sealed class Font : IDisposable
{
    /// <summary>The fontstash context this font was added to.</summary>
    public IntPtr Stash { get; }

    /// <summary>The fontstash font id returned by fonsAddFontMem.</summary>
    public int Id { get; }

    /// <summary>Default render size in logical pixels.</summary>
    public float DefaultSize { get; set; } = 16f;

    private IntPtr _nativeData;
    private bool _disposed;

    internal Font(IntPtr stash, int id, IntPtr nativeData)
    {
        Stash = stash;
        Id = id;
        _nativeData = nativeData;
    }

    public static unsafe Font FromTrueTypeBytes(IntPtr stash, string name, ReadOnlySpan<byte> ttfBytes)
    {
        // Copy into unmanaged memory so fontstash can hold the pointer.
        var nativeData = Marshal.AllocHGlobal(ttfBytes.Length);
        ttfBytes.CopyTo(new Span<byte>((void*)nativeData, ttfBytes.Length));

        int id = fonsAddFontMem(stash, name, (byte*)nativeData, ttfBytes.Length, freeData: 0);
        if (id < 0) // FONS_INVALID
        {
            Marshal.FreeHGlobal(nativeData);
            throw new InvalidOperationException($"fontstash failed to add font '{name}'.");
        }

        return new Font(stash, id, nativeData);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_nativeData != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_nativeData);
            _nativeData = IntPtr.Zero;
        }
        // fontstash itself doesn't have a "remove font" API; atlas memory is
        // reclaimed when sfons_destroy is called on context teardown.
    }
}
