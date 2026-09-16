namespace RenderingLibrary.Graphics.Fonts;

/// <summary>
/// Outcome of <see cref="IRuntimeFontService.CreateFontIfNecessary"/>: whether the requested
/// font file can be loaded from disk now.
/// </summary>
public enum FontFileStatus
{
    /// <summary>The font file exists on disk (it was already there or was just generated).</summary>
    Ready,

    /// <summary>Generation failed; the file may be missing or stale.</summary>
    Failed,

    /// <summary>Another request is generating this font right now, so its files must not be loaded yet.</summary>
    Generating
}
