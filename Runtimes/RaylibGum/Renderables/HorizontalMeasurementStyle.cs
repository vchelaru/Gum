namespace RenderingLibrary.Graphics;

/// <summary>
/// Controls how the width of the last character in a line is measured.
/// This is a RaylibGum-local copy of the enum defined in the
/// RenderingLibrary so that shared code (including linked Forms controls
/// such as TextBoxBase) can reference <c>HorizontalMeasurementStyle</c>
/// without the Raylib project needing to pull in the full BitmapFont
/// machinery. On the Raylib runtime the style parameter is advisory -
/// Raylib's native text engine is used for measurement regardless.
/// </summary>
public enum HorizontalMeasurementStyle
{
    /// <summary>
    /// Uses the character's XAdvance for the last letter, treating it the same as all other letters.
    /// </summary>
    Full = 1,
    /// <summary>
    /// Uses the character's actual pixel width (PixelRight - PixelLeft) for the last letter,
    /// trimming any trailing advance so the measured width fits tightly around the rendered glyphs.
    /// </summary>
    TrimRight = 2,
}
