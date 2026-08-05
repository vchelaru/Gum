using System.Collections.Generic;

namespace RenderingLibrary.Graphics.Fonts;

/// <summary>
/// Font-wide and per-glyph metrics in unscaled font design units, obtained without rasterizing
/// any glyphs (issue #4309). Scales to pixels by exact multiplication
/// (<c>value * fontSizeInPixels / UnitsPerEm</c>), with no per-size hinting/rounding step, so a
/// measurement built from these is stable across any requested pixel size -- unlike a rasterized
/// <see cref="BitmapFont"/>'s metrics, which are independently pixel-snapped per raster size.
/// </summary>
public sealed class FontDesignMetrics
{
    /// <summary>Design units per em square. Typically 1000 or 2048.</summary>
    public required int UnitsPerEm { get; init; }

    /// <summary>Total line height (ascender - descender + line gap), in font design units.</summary>
    public required int LineHeight { get; init; }

    /// <summary>Per-codepoint horizontal metrics, in font design units.</summary>
    public required IReadOnlyDictionary<int, GlyphDesignMetrics> GlyphMetrics { get; init; }
}
