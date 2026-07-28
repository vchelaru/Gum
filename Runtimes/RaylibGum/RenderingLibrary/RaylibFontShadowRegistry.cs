using System.Collections.Generic;

namespace RenderingLibrary;

/// <summary>
/// Associates a Gum bitmap font's drop-shadow companion font (issue #4057, the Raylib counterpart of
/// <c>RenderingLibrary.Graphics.Fonts.BitmapFont.ShadowFont</c>) with the atlas texture id of the
/// loaded primary raylib font.
/// </summary>
/// <remarks>
/// <see cref="Raylib_cs.Font"/> is a value struct with no room for a "shadow companion" field, so the
/// association is kept here instead, keyed by the primary font's atlas texture id - the same approach
/// <see cref="RaylibFontMetricsRegistry"/> uses for line metrics. Entries are removed when the owning
/// <see cref="ManagedFont"/> is disposed (which also unloads the shadow font's own GPU texture), since
/// raylib may later reuse the freed texture id. Accessed only on the single render/content-load thread.
/// </remarks>
internal static class RaylibFontShadowRegistry
{
    private static readonly Dictionary<uint, Font> _shadowFontByPrimaryTextureId = new();

    /// <summary>
    /// Records the shadow companion font for the primary font whose atlas has the given texture id,
    /// overwriting any prior entry for that id.
    /// </summary>
    public static void Register(uint primaryTextureId, Font shadowFont)
    {
        _shadowFontByPrimaryTextureId[primaryTextureId] = shadowFont;
    }

    /// <summary>
    /// Looks up the shadow companion font recorded for the given primary atlas texture id. Returns
    /// false when the primary font has no "-shadow.fnt" sibling (the common, non-dropshadow case).
    /// </summary>
    public static bool TryGet(uint primaryTextureId, out Font shadowFont)
    {
        return _shadowFontByPrimaryTextureId.TryGetValue(primaryTextureId, out shadowFont);
    }

    /// <summary>
    /// Removes and returns the shadow font entry for the given primary atlas texture id, if any.
    /// Called when the owning primary font is unloaded so its shadow companion's GPU texture can be
    /// unloaded too, and a reused texture id cannot return a stale shadow font.
    /// </summary>
    public static bool Remove(uint primaryTextureId, out Font shadowFont)
    {
        if (_shadowFontByPrimaryTextureId.TryGetValue(primaryTextureId, out shadowFont))
        {
            _shadowFontByPrimaryTextureId.Remove(primaryTextureId);
            return true;
        }
        return false;
    }
}
