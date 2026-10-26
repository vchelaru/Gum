using System.Collections.Generic;

namespace RenderingLibrary;

/// <summary>
/// Associates a font's line-spacing metrics (a .fnt's <c>lineHeight</c> and <c>base</c>, or a TTF's loaded
/// through <c>TtfFontLoader</c>) with the atlas texture id of the loaded raylib font.
/// </summary>
/// <remarks>
/// raylib's <see cref="Raylib_cs.Font"/> struct carries only <c>BaseSize</c> — it has no field for a
/// font's lineHeight/baseline — so when a Gum .fnt is assembled or a TTF is loaded through
/// <c>TtfFontLoader</c> those values are recorded here
/// and looked up by the <c>Text</c> renderable when it measures lines. Without this, the renderable
/// reconstructed line height as <c>MeasureTextEx("M") ≈ BaseSize</c>, dropping the descender region
/// that the MonoGame <c>BitmapFont.LineHeightInPixels</c> keeps — so a Text (and any container sized
/// RelativeToChildren around it) came out shorter on raylib than MonoGame.
///
/// Keyed by atlas texture id because that value travels with the (copied) <see cref="Raylib_cs.Font"/>
/// struct through LoaderManager caching and reassignment, whereas the parsed .fnt metadata is
/// discarded. Entries are removed when the owning <see cref="ManagedFont"/> is disposed, since raylib
/// may later reuse the freed texture id. A font freed some other way (raylib's own <c>UnloadFont</c>)
/// leaves its entry behind, so each entry also records the font's BaseSize and atlas size, and a lookup
/// whose font differs in any of them finds nothing. Accessed only on the single render/content-load thread.
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

    private readonly struct Entry
    {
        public readonly FontLineMetrics Metrics;
        public readonly int BaseSize;
        public readonly int TextureWidth;
        public readonly int TextureHeight;

        public Entry(FontLineMetrics metrics, int baseSize, int textureWidth, int textureHeight)
        {
            Metrics = metrics;
            BaseSize = baseSize;
            TextureWidth = textureWidth;
            TextureHeight = textureHeight;
        }
    }

    private static readonly Dictionary<uint, Entry> _entriesByTextureId = new();

    // Whether a font's glyph table has a tab (codepoint 9) row, scanned once per loaded font.
    private readonly record struct TabEntry(int BaseSize, int TextureWidth, int TextureHeight, bool HasTab);

    private static readonly Dictionary<uint, TabEntry> _tabEntriesByTextureId = new();

    /// <summary>
    /// Records the line metrics for a font of <paramref name="baseSize"/> whose atlas is
    /// <paramref name="texture"/>, overwriting any prior entry for that texture id.
    /// </summary>
    public static void Register(Raylib_cs.Texture2D texture, int baseSize, int lineHeight, int baselineY)
    {
        _entriesByTextureId[texture.Id] = new Entry(new FontLineMetrics(lineHeight, baselineY), baseSize, texture.Width, texture.Height);
        _tabEntriesByTextureId.Remove(texture.Id);
    }

    /// <summary>
    /// Whether <paramref name="font"/> has a <c>char id=9</c> row, whose advance is then the tab's width
    /// (as on MonoGame). A font without one gets four spaces. raylib's default font and a
    /// <c>TtfFontLoader</c> font have only printable glyphs, so they never do.
    /// </summary>
    public static unsafe bool HasTabGlyph(Raylib_cs.Font font)
    {
        if (font.Glyphs == null || font.GlyphCount <= 0)
        {
            return false;
        }

        uint id = font.Texture.Id;
        if (_tabEntriesByTextureId.TryGetValue(id, out TabEntry cached)
            && cached.BaseSize == font.BaseSize
            && cached.TextureWidth == font.Texture.Width
            && cached.TextureHeight == font.Texture.Height)
        {
            return cached.HasTab;
        }

        bool hasTab = false;
        for (int i = 0; i < font.GlyphCount; i++)
        {
            if (font.Glyphs[i].Value == '\t')
            {
                hasTab = true;
                break;
            }
        }
        _tabEntriesByTextureId[id] = new TabEntry(font.BaseSize, font.Texture.Width, font.Texture.Height, hasTab);
        return hasTab;
    }

    /// <summary>
    /// Looks up the line metrics recorded for <paramref name="font"/>. Returns false for fonts without
    /// recorded metrics (raylib's built-in default font, a TTF loaded with raylib's own LoadFontEx) and for a
    /// font that only reuses the texture id of a freed one, in which case callers fall back to native measurement.
    /// </summary>
    public static bool TryGet(Raylib_cs.Font font, out FontLineMetrics metrics)
    {
        if (_entriesByTextureId.TryGetValue(font.Texture.Id, out Entry entry)
            && entry.BaseSize == font.BaseSize
            && entry.TextureWidth == font.Texture.Width
            && entry.TextureHeight == font.Texture.Height)
        {
            metrics = entry.Metrics;
            return true;
        }
        metrics = default;
        return false;
    }

    /// <summary>
    /// Whether any metrics are recorded under the given atlas texture id.
    /// </summary>
    public static bool IsRegistered(uint textureId)
    {
        return _entriesByTextureId.ContainsKey(textureId);
    }

    /// <summary>
    /// Removes the entry for the given atlas texture id. Called when the owning font is unloaded so a
    /// reused texture id cannot return stale metrics.
    /// </summary>
    public static void Remove(uint textureId)
    {
        _entriesByTextureId.Remove(textureId);
        _tabEntriesByTextureId.Remove(textureId);
    }
}
