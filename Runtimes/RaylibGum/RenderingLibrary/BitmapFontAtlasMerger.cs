using System;

namespace RenderingLibrary.Content;

/// <summary>
/// Merges the pages of a multi-page bitmap font into one RGBA atlas. A raylib <c>Font</c> holds a
/// single texture, so every page is stacked vertically into it and each glyph's atlas Y is shifted
/// by its page's offset. Pure pixel and rectangle math with no raylib or GL calls, so it runs
/// without a window.
/// </summary>
internal static class BitmapFontAtlasMerger
{
    internal const int BytesPerPixel = 4;

    /// <summary>
    /// The size of the stacked atlas and where each page starts in it.
    /// </summary>
    internal sealed class Layout
    {
        public Layout(int width, int height, int[] pageYOffsets)
        {
            Width = width;
            Height = height;
            PageYOffsets = pageYOffsets;
        }

        public int Width { get; }
        public int Height { get; }

        /// <summary>The atlas Y of each page's top row, indexed by page.</summary>
        public int[] PageYOffsets { get; }
    }

    /// <summary>
    /// Stacks pages top to bottom. The atlas is as wide as the widest page; a narrower page leaves
    /// transparent pixels to its right.
    /// </summary>
    internal static Layout ComputeLayout(int[] pageWidths, int[] pageHeights)
    {
        if (pageWidths.Length == 0)
        {
            throw new ArgumentException("A bitmap font needs at least one page.", nameof(pageWidths));
        }
        if (pageWidths.Length != pageHeights.Length)
        {
            throw new ArgumentException("pageWidths and pageHeights must have the same length.", nameof(pageHeights));
        }

        int width = 0;
        int[] yOffsets = new int[pageWidths.Length];
        int nextY = 0;
        for (int i = 0; i < pageWidths.Length; i++)
        {
            if (pageWidths[i] <= 0 || pageHeights[i] <= 0)
            {
                throw new ArgumentException($"Page {i} has an invalid size {pageWidths[i]}x{pageHeights[i]}.");
            }
            yOffsets[i] = nextY;
            nextY = checked(nextY + pageHeights[i]);
            width = System.Math.Max(width, pageWidths[i]);
        }
        return new Layout(width, nextY, yOffsets);
    }

    /// <summary>
    /// Copies each page's RGBA pixels into a new atlas buffer laid out by <paramref name="layout"/>.
    /// </summary>
    internal static byte[] MergeRgba(byte[][] pagePixels, int[] pageWidths, int[] pageHeights, Layout layout)
    {
        byte[] atlas = new byte[checked(layout.Width * layout.Height * BytesPerPixel)];
        int atlasStride = layout.Width * BytesPerPixel;
        for (int page = 0; page < pagePixels.Length; page++)
        {
            int rowBytes = pageWidths[page] * BytesPerPixel;
            if (pagePixels[page].Length < rowBytes * pageHeights[page])
            {
                throw new ArgumentException($"Page {page} has fewer pixels than its {pageWidths[page]}x{pageHeights[page]} size.");
            }
            for (int row = 0; row < pageHeights[page]; row++)
            {
                Buffer.BlockCopy(
                    pagePixels[page], row * rowBytes,
                    atlas, (layout.PageYOffsets[page] + row) * atlasStride,
                    rowBytes);
            }
        }
        return atlas;
    }
}
