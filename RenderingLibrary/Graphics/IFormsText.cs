using System.Collections.Generic;

namespace RenderingLibrary.Graphics
{
    /// <summary>
    /// Forms-control-facing extension of <see cref="IWrappedText"/>. Adds the subset of
    /// concrete <c>Text</c> members that <c>TextBoxBase</c>, <c>MenuItem</c>, and
    /// <c>DialogBox</c> in GumCommon need to reach — the wrapped-lines view, alignment,
    /// typewriter-style letter revealing, line-height multiplier, and the
    /// measurement-style-aware overload of MeasureString.
    /// </summary>
    /// <remarks>
    /// Inherits <see cref="IWrappedText"/> (which itself extends <see cref="IText"/>) so
    /// callers get RawText / FontScale / Width / MeasureString(string) / LineHeightInPixels /
    /// the UpdateLines extension for free. Also inherits <see cref="IRenderableIpso"/> so
    /// callers can reach the Text's positioning (X/Y/Parent and the GetAbsolute* extension
    /// methods) without an additional cast.
    /// </remarks>
    public interface IFormsText : IWrappedText, IRenderableIpso
    {
        /// <summary>
        /// The current wrapped lines after layout. Each entry is one rendered line.
        /// Implementers expose their internal cache; do not mutate.
        /// </summary>
        List<string> WrappedText { get; }

        /// <summary>
        /// Horizontal alignment within the text's layout box.
        /// </summary>
        HorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>
        /// Vertical alignment within the text's layout box.
        /// </summary>
        VerticalAlignment VerticalAlignment { get; set; }

        /// <summary>
        /// Maximum number of letters to render, or <c>null</c> to render the whole string.
        /// Used by typewriter-style effects (e.g. <c>DialogBox</c>) to reveal characters
        /// progressively.
        /// </summary>
        int? MaxLettersToShow { get; set; }

        /// <summary>
        /// Maximum number of wrapped lines to render, or <c>null</c> to render all lines.
        /// Shadows the get-only <see cref="IWrappedText.MaxNumberOfLines"/> with a settable
        /// form — the concrete <c>Text</c> implementations expose both, and Forms controls
        /// need to be able to push values down.
        /// </summary>
        new int? MaxNumberOfLines { get; set; }

        /// <summary>
        /// Scalar applied to <see cref="IWrappedText.LineHeightInPixels"/> when laying out lines.
        /// </summary>
        float LineHeightMultiplier { get; set; }

        /// <summary>
        /// Measures the rendered width of a string using this Text's active font and
        /// the supplied measurement style. Result is in raw glyph pixels — callers that
        /// render in screen space must multiply by <see cref="IText.FontScale"/>.
        /// </summary>
        float MeasureString(string value, HorizontalMeasurementStyle style);

        /// <summary>
        /// The horizontal advance of a single character in the font this Text measures layout with,
        /// in raw glyph pixels (before <see cref="IText.FontScale"/>). The default implementation
        /// measures the character via <see cref="IWrappedText.MeasureString(string)"/>; backends with
        /// cheap per-glyph metrics (e.g. a bitmap font's XAdvance table) override this for an
        /// allocation-free lookup. Positions within a line come from <see cref="GetDrawnCharacterAdvance"/>.
        /// </summary>
        float GetCharacterAdvance(char character) => MeasureString(character.ToString());

        /// <summary>
        /// The width of <paramref name="value"/> as this Text draws it, in the units the Text is positioned
        /// in (after <see cref="IText.FontScale"/>). Every position within a line comes from this and
        /// <see cref="GetDrawnCharacterAdvance"/>: the caret, a selection, a click, a line's alignment offset.
        /// Layout (wrapping, sizing to the text) uses <see cref="MeasureString(string, HorizontalMeasurementStyle)"/>,
        /// which differs while font oversampling draws from a larger font than layout measures with (#7675).
        /// </summary>
        float MeasureDrawnString(string value, HorizontalMeasurementStyle style) =>
            MeasureString(value, style) * WordWrapFontScale;

        /// <summary>
        /// The advance of a single character as this Text draws it, in the units the Text is positioned in.
        /// Used by hit testing to walk a line one character at a time without measuring substrings.
        /// See <see cref="MeasureDrawnString"/>.
        /// </summary>
        float GetDrawnCharacterAdvance(char character) => GetCharacterAdvance(character) * WordWrapFontScale;

        /// <summary>
        /// The distance from the top of the first drawn line to the top of line <paramref name="lineIndex"/>
        /// (<c>WrappedText.Count</c> gives the drawn height), in the units the Text is positioned in. A Text
        /// returns where it draws each line (its measurement font and <see cref="LineHeightMultiplier"/>, and a
        /// taller line where a size run makes it so) rather than the display font, which differs while font
        /// oversampling draws from a larger font than layout measures with (#7705). Every vertical position across
        /// lines comes from this: caret, click, selection rows, Up/Down and scrolling.
        /// </summary>
        float GetDrawnLineTop(int lineIndex) => lineIndex * LineHeightInPixels * WordWrapFontScale * LineHeightMultiplier;

        /// <summary>
        /// The height of one drawn glyph row, without the <see cref="LineHeightMultiplier"/> (which only
        /// spaces lines apart): the right size for "half a line" offsets onto the ink.
        /// </summary>
        float DrawnGlyphRowHeight => LineHeightInPixels * WordWrapFontScale;
    }
}
