using System.Collections.Generic;

namespace RenderingLibrary.Graphics.Fonts;

/// <summary>
/// Optional companion to <see cref="IInMemoryFontCreator"/> (issue #4542): implemented by an in-memory
/// font creator that can grow a font it previously produced with new characters, in place, without a
/// full regenerate. Consumers (e.g. <c>TextRuntime</c>'s automatic glyph-growth trigger) check
/// <c>CustomSetPropertyOnRenderable.InMemoryFontCreator</c> for this interface at the point they
/// need to grow a font -- an <see cref="IInMemoryFontCreator"/> that doesn't implement it simply
/// doesn't support growth, the same "not applicable" signal <see cref="IInMemoryFontCreator.TryCreateFont"/>
/// itself uses for a null return.
/// </summary>
public interface IGrowableFontCreator
{
    /// <summary>
    /// Attempts to add <paramref name="characters"/> to <paramref name="font"/>, a font this creator
    /// previously produced via <see cref="IInMemoryFontCreator.TryCreateFont"/>, growing its live
    /// texture(s) in place instead of a full regenerate.
    /// </summary>
    /// <param name="font">The font to grow, as returned by an earlier <see cref="IInMemoryFontCreator.TryCreateFont"/> call.</param>
    /// <param name="bmfcSave">The same descriptor <paramref name="font"/> was created from.</param>
    /// <param name="characters">The characters to add. Already-present characters are a silent no-op.</param>
    /// <returns>
    /// <c>null</c> when <paramref name="font"/> was not created by this creator, or growth is not
    /// supported for it (e.g. it was generated from a system font with no backing file). Otherwise, a
    /// possibly-empty list of the requested characters this font cannot render at all -- e.g. no glyph
    /// for them exists in the source font file.
    /// </returns>
    IReadOnlyList<char>? TryAddGlyphs(BitmapFont font, BmfcSave bmfcSave, string characters);
}
