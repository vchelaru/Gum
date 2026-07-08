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
        /// The horizontal advance of a single character in this Text's active font, in raw
        /// glyph pixels (before <see cref="IText.FontScale"/>) — used by caret hit-testing
        /// (<c>TextBoxBase.GetIndex</c>) to walk a string one character at a time without
        /// repeated substring measurement. The default implementation measures the character
        /// via <see cref="IWrappedText.MeasureString(string)"/>; backends with cheap per-glyph
        /// metrics (e.g. a bitmap font's XAdvance table) override this for an allocation-free
        /// lookup.
        /// </summary>
        float GetCharacterAdvance(char character) => MeasureString(character.ToString());
    }
}
