using System.Runtime.InteropServices;
using static Sokol.Fontstash;

namespace SokolGum;

/// <summary>
/// A font loaded into a <see cref="FontAtlas"/>'s fontstash context.
/// Holds the numeric font id that fontstash returned so <see cref="Text"/>
/// can select it via <c>fonsSetFont</c>.
///
/// Font is intentionally NOT <see cref="IDisposable"/>. fontstash has no
/// per-font removal API, and the unmanaged TTF byte buffer it reads glyph
/// outlines from is owned by <see cref="FontAtlas"/> (so it's released
/// only when the atlas itself is disposed). Individual Font instances
/// therefore own nothing — lifetime is tied to the atlas. If callers
/// want to `using` a font, they're almost certainly doing something
/// wrong; don't offer the pattern.
/// </summary>
public sealed class Font
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
    /// management — the returned Font stays valid until the atlas itself
    /// is disposed.
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
}
