namespace RenderingLibrary.Graphics.Fonts;

/// <summary>
/// Per-glyph horizontal metrics in unscaled font design units (the same space as
/// <see cref="FontDesignMetrics.UnitsPerEm"/>) -- i.e. straight from the font file, before any
/// rasterization/hinting. Mirrors KernSmith's own GlyphDesignMetrics shape so
/// <c>RenderingLibrary</c> does not need to reference KernSmith directly.
/// </summary>
/// <param name="AdvanceWidth">Horizontal advance to the next glyph, in font design units.</param>
/// <param name="LeftSideBearing">Distance from the glyph origin to the left edge of the glyph's bounding box, in font design units.</param>
public readonly record struct GlyphDesignMetrics(int AdvanceWidth, int LeftSideBearing);
