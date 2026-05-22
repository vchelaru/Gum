namespace RenderingLibrary.Graphics;

/// <summary>
/// Controls how the width of the last character in a line is measured.
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
    // eventually trim left and trim both too
}
