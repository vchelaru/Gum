using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics
{
    public interface IText
    {
        void SetNeedsRefreshToTrue();

        void UpdatePreRenderDimensions();

        /// <summary>
        /// The distance from the baseline to the bottom of the line, in unscaled pixels. Every
        /// implementation must return it without <see cref="FontScale"/> applied, even when its own
        /// text measurement already includes the scale. Layout multiplies it by <see cref="FontScale"/>
        /// wherever it positions against the baseline (the TextBaseline origin, PixelsFromBaseline
        /// children, and content sizing).
        /// </summary>
        float DescenderHeight { get; }
        float FontScale { get; }

        float WrappedTextWidth { get; }
        float WrappedTextHeight { get; }

        // The text that was assgined on this Text instance prior to any wrapping.
        string? RawText { get; set; }

        string? StoredMarkupText { get; }

        float? Width { get; set; }

        TextOverflowVerticalMode TextOverflowVerticalMode { get; set; }
    }

    /// <summary>
    /// The one formula for how tall a text line is when it may hold a size run ([FontScale]/[FontSize]),
    /// shared by every runtime (MonoGame/KNI/FNA, Raylib, Skia) so the drawn advance and the reported
    /// Height cannot disagree between them (#6459).
    /// </summary>
    public static class TextLineHeight
    {
        /// <summary>
        /// The larger of the multiplied base (<paramref name="baseScale"/> times
        /// <paramref name="lineHeightMultiplier"/>) and the tallest size run's scale, times
        /// <paramref name="lineHeightInPixels"/>. A run's scale is absolute and is never multiplied, so
        /// <c>[FontScale=3]</c> stays three lines tall at any multiplier, and a line with no size run
        /// (<paramref name="tallestSizeRunScale"/> 0) is the multiplied base.
        /// </summary>
        public static float Compute(float lineHeightInPixels, float baseScale, float lineHeightMultiplier, float tallestSizeRunScale)
        {
            return lineHeightInPixels * System.Math.Max(baseScale * lineHeightMultiplier, tallestSizeRunScale);
        }
    }
}
