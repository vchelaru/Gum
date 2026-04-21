namespace RenderingLibrary.Graphics;

/// <summary>
/// Controls how the width of the last character in a line is measured.
/// This is a SokolGum-local copy of the enum defined in the
/// RenderingLibrary (alongside BitmapFont) so that shared code
/// (including linked Forms controls such as TextBoxBase) can reference
/// <c>HorizontalMeasurementStyle</c> without the Sokol project needing
/// to pull in the full BitmapFont machinery. On the Sokol runtime the
/// style parameter is advisory - fontstash's native text engine is used
/// for measurement regardless. Mirrors the RaylibGum approach.
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
