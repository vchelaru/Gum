namespace RenderingLibrary.Graphics.Fonts;

/// <summary>
/// Minimal font service interface for runtime font generation.
/// Both the Gum tool and game runtimes can implement this to enable
/// on-demand font creation in shared rendering code.
/// </summary>
public interface IRuntimeFontService
{
    /// <summary>
    /// The absolute path to the font cache folder for the current project.
    /// </summary>
    string AbsoluteFontCacheFolder { get; }

    /// <summary>
    /// Synchronously creates a font file described by <paramref name="bmfcSave"/>
    /// if it does not already exist on disk.
    /// </summary>
    void CreateFontIfNecessary(BmfcSave bmfcSave);
}
