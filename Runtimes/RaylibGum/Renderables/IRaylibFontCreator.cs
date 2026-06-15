using RenderingLibrary.Graphics.Fonts;

namespace RaylibGum.Renderables;

/// <summary>
/// Optional in-memory font creator for the Raylib runtime. When assigned to
/// <see cref="CustomSetPropertyOnRenderable.InMemoryFontCreator"/>, font generation bypasses disk
/// entirely — the creator produces a <see cref="Raylib_cs.Font"/> directly from a
/// <see cref="BmfcSave"/> descriptor (for example, by rasterizing an atlas with KernSmith).
/// </summary>
/// <remarks>
/// This is the Raylib parallel to <see cref="IInMemoryFontCreator"/>. The two cannot be unified:
/// <see cref="IInMemoryFontCreator"/> returns a MonoGame-family <c>BitmapFont</c>, which Raylib's
/// text renderer cannot draw — Raylib renders <see cref="Raylib_cs.Font"/>.
/// </remarks>
public interface IRaylibFontCreator
{
    /// <summary>
    /// Attempts to create a <see cref="Raylib_cs.Font"/> for the given font descriptor. Returns
    /// <c>null</c> when the font cannot be produced, so the caller falls through to the existing
    /// disk / system-font path.
    /// </summary>
    /// <param name="bmfcSave">The font descriptor (family, size, style, outline, ranges, spacing).</param>
    Raylib_cs.Font? TryCreateFont(BmfcSave bmfcSave);
}
