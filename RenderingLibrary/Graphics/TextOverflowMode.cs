namespace RenderingLibrary.Graphics
{
    public enum TextOverflowHorizontalMode
    {
        TruncateWord,
        EllipsisLetter,
        // eventually?
        //ScaleToFit
    }

    public enum TextOverflowVerticalMode
    {
        SpillOver,
        TruncateLine
    }
}

namespace Gum.Graphics
{
    /// <summary>
    /// Controls the order in which letters are rendered within a line of text, which
    /// determines how adjacent glyphs overlap when their bounding boxes intersect
    /// (for example, italic fonts, ligatures, or letters with exaggerated kerning).
    /// </summary>
    /// <remarks>
    /// Only the pixels that actually overlap differ between modes. For non-overlapping
    /// glyphs the result is visually identical.
    /// </remarks>
    public enum OverlapDirection
    {
        /// <summary>
        /// Letters are drawn left-to-right. When two glyphs overlap, the one on the
        /// right is drawn on top of the one on the left. This is the default.
        /// </summary>
        RightOnTop,

        /// <summary>
        /// Letters are drawn right-to-left. When two glyphs overlap, the one on the
        /// left is drawn on top of the one on the right.
        /// </summary>
        LeftOnTop
    }
}