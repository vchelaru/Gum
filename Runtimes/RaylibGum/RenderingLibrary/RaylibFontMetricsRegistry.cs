using System.Collections.Generic;

namespace RenderingLibrary;

/// <summary>
/// Associates a Gum bitmap font's line-spacing metrics (the .fnt <c>lineHeight</c> and <c>base</c>)
/// with the atlas texture id of the loaded raylib font.
/// </summary>
/// <remarks>
/// raylib's <see cref="Raylib_cs.Font"/> struct carries only <c>BaseSize</c> — it has no field for a
/// bitmap font's lineHeight/baseline — so when a Gum .fnt is assembled those values are recorded here
/// and looked up by the <c>Text</c> renderable when it measures lines. Without this, the renderable
/// reconstructed line height as <c>MeasureTextEx("M") ≈ BaseSize</c>, dropping the descender region
/// that the MonoGame <c>BitmapFont.LineHeightInPixels</c> keeps — so a Text (and any container sized
/// RelativeToChildren around it) came out shorter on raylib than MonoGame.
///
/// Keyed by atlas texture id because that value travels with the (copied) <see cref="Raylib_cs.Font"/>
/// struct through LoaderManager caching and reassignment, whereas the parsed .fnt metadata is
/// discarded. Entries are removed when the owning <see cref="ManagedFont"/> is disposed, since raylib
/// may later reuse the freed texture id. Accessed only on the single render/content-load thread.
/// </remarks>
internal static class RaylibFontMetricsRegistry
{
    /// <summary>
    /// The line-spacing metrics recorded for one loaded bitmap font.
    /// </summary>
    public readonly struct FontLineMetrics
    {
        /// <summary>The full line height in pixels (the .fnt <c>common lineHeight</c>).</summary>
        public int LineHeight { get; }

        /// <summary>The distance in pixels from the top of the line to the baseline (the .fnt <c>common base</c>).</summary>
        public int BaselineY { get; }

        /// <summary>Initializes a new instance of the <see cref="FontLineMetrics"/> struct.</summary>
        public FontLineMetrics(int lineHeight, int baselineY)
        {
            LineHeight = lineHeight;
            BaselineY = baselineY;
        }
    }

    private static readonly Dictionary<uint, FontLineMetrics> _metricsByTextureId = new();

    /// <summary>
    /// Records the line metrics for the font whose atlas has the given texture id, overwriting any
    /// prior entry for that id.
    /// </summary>
    public static void Register(uint textureId, int lineHeight, int baselineY)
    {
        _metricsByTextureId[textureId] = new FontLineMetrics(lineHeight, baselineY);
    }

    /// <summary>
    /// Looks up the line metrics recorded for the given atlas texture id. Returns false for fonts that
    /// were not assembled from a Gum .fnt (raylib's built-in default font, TTF loaded via LoadFontEx),
    /// in which case callers fall back to native measurement.
    /// </summary>
    public static bool TryGet(uint textureId, out FontLineMetrics metrics)
    {
        return _metricsByTextureId.TryGetValue(textureId, out metrics);
    }

    /// <summary>
    /// Removes the entry for the given atlas texture id. Called when the owning font is unloaded so a
    /// reused texture id cannot return stale metrics.
    /// </summary>
    public static void Remove(uint textureId)
    {
        _metricsByTextureId.Remove(textureId);
    }
}
