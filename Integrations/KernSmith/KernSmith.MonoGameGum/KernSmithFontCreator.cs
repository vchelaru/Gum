using KernSmith.Atlas;
using KernSmith.Output;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace KernSmith.Gum;

/// <summary>
/// Creates <see cref="BitmapFont"/> instances in memory using KernSmith for Gum games.
/// Generates font textures and metadata without any disk I/O.
/// </summary>
public class KernSmithFontCreator : IInMemoryFontCreator
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly RasterizerBackend? _backend;

    /// <summary>
    /// Initializes a new instance of <see cref="KernSmithFontCreator"/>.
    /// </summary>
    /// <param name="graphicsDevice">
    /// The graphics device used to create font textures.
    /// </param>
    /// <param name="backend">
    /// Optional rasterizer backend override. When null, uses the default (FreeType).
    /// Use <see cref="RasterizerBackend.StbTrueType"/> on platforms where native
    /// libraries are unavailable (e.g., Blazor WASM).
    /// </param>
    public KernSmithFontCreator(GraphicsDevice graphicsDevice, RasterizerBackend? backend = null)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _backend = backend;
    }

    /// <summary>
    /// Registers raw font data (TTF/OTF/WOFF) under a family name so that
    /// font generation can resolve it without accessing system fonts.
    /// </summary>
    /// <param name="familyName">Font family name (e.g., "Arial").</param>
    /// <param name="fontData">Raw font file bytes.</param>
    /// <param name="style">
    /// Optional style name (e.g., "Bold", "Italic", "Bold Italic").
    /// When null, registers as the default/regular variant.
    /// </param>
    /// <param name="faceIndex">TTC face index (0 for single-face font files).</param>
    public static void RegisterFont(string familyName, byte[] fontData, string? style = null, int faceIndex = 0)
        => BmFont.RegisterFont(familyName, fontData, style, faceIndex);

    /// <summary>
    /// Registers a font file under a family name, reading it through
    /// <see cref="FileManager.GetStreamForFile"/> so a font that only exists behind a host stream
    /// hook (a .ttf packed into a .gumpkg, or one in a game's asset zip) registers the same as one
    /// on disk, and otherwise via TitleContainer.OpenStream, which resolves content files correctly
    /// on all platforms (desktop, Android, iOS, consoles).
    /// </summary>
    /// <param name="familyName">Font family name (e.g., "Arial").</param>
    /// <param name="filePath">
    /// Path to a .ttf, .otf, or .woff font file, relative to the
    /// title container root (typically the Content directory).
    /// </param>
    /// <param name="style">
    /// Optional style name (e.g., "Bold", "Italic", "Bold Italic").
    /// When null, registers as the default/regular variant.
    /// </param>
    /// <param name="faceIndex">TTC face index (0 for single-face font files).</param>
    public static void RegisterFont(string familyName, string filePath,
        string? style = null, int faceIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(familyName);
        ArgumentNullException.ThrowIfNull(filePath);

        BmFont.RegisterFont(familyName, ReadFontBytes(filePath), style, faceIndex);
    }

    /// <summary>
    /// Prefers FileManager's stream hook so a bundled font registers like one on disk (#4523),
    /// falling back to the title container: FileManager routes exclusively to the hook once one is
    /// installed, and a hook that doesn't carry this font must not hide the shipped copy.
    /// </summary>
    private static byte[] ReadFontBytes(string filePath)
    {
        if (FileManager.CustomGetStreamFromFile != null)
        {
            try
            {
                return GumFontGenerator.ReadFontFileBytes(filePath);
            }
            catch (IOException)
            {
                // Not in the hook and not at the absolute path it resolves to; the title container
                // below still knows where this platform keeps its content.
            }
        }

        using Stream stream = TitleContainer.OpenStream(filePath);
        using MemoryStream memoryStream = new();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Removes a previously registered font.
    /// </summary>
    /// <param name="familyName">Font family name.</param>
    /// <param name="style">Optional style name, or null for the default variant.</param>
    /// <returns>True if a font was removed.</returns>
    public static bool UnregisterFont(string familyName, string? style = null)
        => BmFont.UnregisterFont(familyName, style);

    /// <summary>
    /// Removes all registered fonts.
    /// </summary>
    public static void ClearRegisteredFonts()
        => BmFont.ClearRegisteredFonts();

    /// <inheritdoc/>
    public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
    {
        BmFontResult result = GumFontGenerator.Generate(bmfcSave, _backend);

        string baseName = System.IO.Path.GetFileNameWithoutExtension(bmfcSave.FontCacheFileName);

        Texture2D[] textures = new Texture2D[result.Pages.Count];
        for (int i = 0; i < result.Pages.Count; i++)
        {
            AtlasPage page = result.Pages[i];
            Texture2D texture = new Texture2D(_graphicsDevice, page.Width, page.Height,
                false, SurfaceFormat.Color);
            texture.Name = $"{baseName}_{i}";
            texture.SetData(page.PixelData);
            textures[i] = texture;
        }

        BitmapFont bitmapFont = new BitmapFont(textures, result.FntText);

        // Issue #4061: attach the shadow AtlasVariant (if requested) as ShadowFont. It shares the
        // primary's atlas pages (same shared texture array), so no extra texture upload is needed.
        if (result.VariantModels.ContainsKey("shadow"))
        {
            bitmapFont.ShadowFont = new BitmapFont(textures, result.GetVariantFntText("shadow"));
        }

        return bitmapFont;
    }
}
