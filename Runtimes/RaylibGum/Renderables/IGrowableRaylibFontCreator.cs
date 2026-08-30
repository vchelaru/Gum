using System.Collections.Generic;
using RenderingLibrary.Graphics.Fonts;

namespace RaylibGum.Renderables;

/// <summary>
/// Optional companion to <see cref="IRaylibFontCreator"/> (issue #4546, raylib parity for #4542):
/// implemented by an in-memory raylib font creator that can grow a font it previously produced with
/// new characters, in place, without a full regenerate. Consumers (<c>TextRuntime</c>'s automatic
/// glyph-growth trigger) check <c>CustomSetPropertyOnRenderable.InMemoryFontCreator</c> for this
/// interface at the point they need to grow a font -- an <see cref="IRaylibFontCreator"/> that
/// doesn't implement it simply doesn't support growth, the same "not applicable" signal
/// <see cref="IRaylibFontCreator.TryCreateFont"/> itself uses for a null return.
/// </summary>
/// <remarks>
/// Unlike <c>RenderingLibrary.Graphics.Fonts.IGrowableFontCreator</c> (the MonoGame/KniGum/FnaGum
/// equivalent), <paramref name="font"/> below is passed by <c>ref</c> rather than mutated through a
/// held reference: <c>BitmapFont</c> is a class, so every <c>Text</c> sharing one automatically sees
/// growth through that shared reference, but <see cref="Raylib_cs.Font"/> is a value type -- each
/// <c>Text</c> holds its own independent copy. A creator that supports growth returns the CURRENT
/// canonical font for this identity (which the caller assigns back onto its own copy via
/// <paramref name="font"/>), and never frees or replaces the resources behind any font it has already
/// handed out -- another <c>Text</c> instance holding an older copy of the same identity keeps
/// rendering it safely until (if ever) its own growth check catches it up to the latest characters.
/// </remarks>
public interface IGrowableRaylibFontCreator
{
    /// <summary>
    /// Attempts to add <paramref name="characters"/> to the font identified by <paramref name="bmfcSave"/>,
    /// growing it in place (via an incremental atlas session) instead of a full regenerate.
    /// </summary>
    /// <param name="font">
    /// In: the caller's own copy of a font this creator previously produced via
    /// <see cref="IRaylibFontCreator.TryCreateFont"/>. Out: replaced with the creator's current
    /// canonical font for this identity when growth is supported (whether or not this specific call
    /// added anything new); left untouched when it returns <c>null</c>.
    /// </param>
    /// <param name="bmfcSave">The same descriptor <paramref name="font"/> was created from.</param>
    /// <param name="characters">The characters to add. Already-present characters are a silent no-op.</param>
    /// <returns>
    /// <c>null</c> when <paramref name="font"/> was not created by this creator, or growth is not
    /// supported for it (e.g. it was generated from a system font with no backing file). Otherwise, a
    /// possibly-empty list of the requested characters this font cannot render at all -- e.g. no glyph
    /// for them exists in the source font file.
    /// </returns>
    IReadOnlyList<char>? TryAddGlyphs(ref Raylib_cs.Font font, BmfcSave bmfcSave, string characters);
}
