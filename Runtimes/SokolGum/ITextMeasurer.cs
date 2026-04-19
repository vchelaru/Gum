namespace SokolGum;

/// <summary>
/// Abstraction over text measurement (line widths, vertical metrics) used
/// by <see cref="Gum.Renderables.Text"/> for layout-time size queries.
/// Decouples measurement from rasterization so headless tests can plug
/// deterministic metrics without standing up fontstash + a GPU context.
/// Production wires <see cref="FontstashTextMeasurer"/> in
/// <see cref="SystemManagers.Initialize"/>.
/// </summary>
public interface ITextMeasurer
{
    /// <summary>
    /// Vertical font metrics in logical pixels at the requested size.
    /// <paramref name="lineHeight"/> is the inter-line step (font-native
    /// line height, before <c>LineHeightMultiplier</c> scaling).
    /// </summary>
    void GetVerticalMetrics(Font? font, float fontSize,
        out float ascent, out float descent, out float lineHeight);

    /// <summary>
    /// Advance width of a run of text in logical pixels. Does not account
    /// for wrapping — callers split on newlines / word-break before calling.
    /// </summary>
    float MeasureLineWidth(Font? font, float fontSize, ReadOnlySpan<char> text);
}
