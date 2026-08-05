namespace RenderingLibrary.Graphics.Fonts;

/// <summary>
/// Creates <see cref="BitmapFont"/> instances entirely in memory, without writing or reading
/// font files from disk. Platform-specific implementations produce texture objects from
/// raw pixel data and construct the <see cref="BitmapFont"/> directly.
/// </summary>
public interface IInMemoryFontCreator
{
    /// <summary>
    /// Attempts to create a <see cref="BitmapFont"/> from the given font description.
    /// Returns <c>null</c> if creation fails or is not supported for the given parameters.
    /// </summary>
    BitmapFont? TryCreateFont(BmfcSave bmfcSave);

    /// <summary>
    /// Attempts to read this font's unscaled design-unit metrics without rasterizing any glyphs
    /// (issue #4309). Returns <c>null</c> if unsupported for the given parameters -- callers must
    /// fall back to measuring against a rasterized <see cref="BitmapFont"/> instead.
    /// </summary>
    FontDesignMetrics? TryGetDesignMetrics(BmfcSave bmfcSave) => null;
}
