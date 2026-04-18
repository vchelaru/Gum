using System.Runtime.InteropServices;
using static Sokol.Fontstash;

namespace SokolGum;

/// <summary>
/// A font loaded into a <see cref="FontAtlas"/>'s fontstash context.
/// Holds the numeric font id that fontstash returned so <see cref="Text"/>
/// can select it via <c>fonsSetFont</c>.
///
/// The unmanaged TTF byte buffer fontstash reads glyph outlines from is
/// owned by <see cref="FontAtlas"/>, not by Font. Fontstash keeps the
/// pointer alive for the context's lifetime, so freeing it as part of
/// Font.Dispose (per-font) would leave later glyph rasterizations
/// pointing at reclaimed memory. Tying the buffer to the atlas means
/// every Font stays valid until the atlas itself is torn down.
/// </summary>
public sealed class Font : IDisposable
{
    /// <summary>The fontstash context this font was added to.</summary>
    public IntPtr Stash { get; }

    /// <summary>The fontstash font id returned by fonsAddFontMem.</summary>
    public int Id { get; }

    /// <summary>Default render size in logical pixels.</summary>
    public float DefaultSize { get; set; } = 16f;

    internal Font(IntPtr stash, int id)
    {
        Stash = stash;
        Id = id;
    }

    /// <summary>
    /// Adds a TTF blob to the atlas's fontstash context. The unmanaged copy
    /// of <paramref name="ttfBytes"/> is handed to the atlas for lifetime
    /// management — don't dispose the returned Font before the atlas itself.
    /// </summary>
    public static unsafe Font FromTrueTypeBytes(FontAtlas atlas, string name, ReadOnlySpan<byte> ttfBytes)
    {
        if (atlas is null) throw new ArgumentNullException(nameof(atlas));

        var nativeData = Marshal.AllocHGlobal(ttfBytes.Length);
        ttfBytes.CopyTo(new Span<byte>((void*)nativeData, ttfBytes.Length));

        int id = fonsAddFontMem(atlas.Stash, name, (byte*)nativeData, ttfBytes.Length, freeData: 0);
        if (id < 0) // FONS_INVALID
        {
            Marshal.FreeHGlobal(nativeData);
            throw new InvalidOperationException($"fontstash failed to add font '{name}'.");
        }

        atlas.TrackFontData(nativeData);
        return new Font(atlas.Stash, id);
    }

    /// <summary>
    /// No-op in practice: Font owns no unmanaged resources of its own, and
    /// fontstash has no "remove single font" API. Kept on the type for
    /// IDisposable symmetry with other loaded content (Texture2D, etc.)
    /// so caller code using <c>using</c> / <c>Dispose</c> patterns compiles.
    /// The actual font buffer and fontstash context are released when the
    /// owning <see cref="FontAtlas"/> is disposed.
    /// </summary>
    public void Dispose() { }
}
