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
    /// Path to a .ttf, .otf, or .woff font file, relative to <see cref="FileManager.RelativeDirectory"/>
    /// - the same base a <c>Font</c>/<c>CustomFontFile</c> path uses (#4527).
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
    /// Resolves <paramref name="filePath"/> from <see cref="FileManager.RelativeDirectory"/> via
    /// <see cref="GumFontGenerator.ReadFontFileBytes"/> - the stream hook when one is installed,
    /// otherwise disk - the same base and resolution order <c>Font</c>/<c>CustomFontFile</c> use.
    /// Falls back to <see cref="TitleContainer"/>, which knows where content lives on platforms
    /// plain file I/O can't reach (Android, iOS, consoles); the fallback path is translated to stay
    /// relative to <see cref="FileManager.RelativeDirectory"/> too, so both routes accept the same
    /// string for the same file (#4527). The translation only applies when RelativeDirectory sits
    /// under the executable directory (the common desktop case); elsewhere <paramref name="filePath"/>
    /// is handed to TitleContainer unchanged, matching prior behavior.
    /// </summary>
    private static byte[] ReadFontBytes(string filePath)
    {
        try
        {
            return GumFontGenerator.ReadFontFileBytes(filePath);
        }
        catch (IOException)
        {
            // Not behind the hook and not on disk at the FileManager.RelativeDirectory-resolved
            // path; fall through to the title container below.
        }

        string titleContainerPath = filePath;
        if (FileManager.IsRelative(filePath))
        {
            string absolutePath = FileManager.MakeAbsolute(filePath);
            string exeLocation = FileManager.ExeLocation;
            if (absolutePath.StartsWith(exeLocation, StringComparison.OrdinalIgnoreCase))
            {
                titleContainerPath = absolutePath.Substring(exeLocation.Length).Replace('\\', '/');
            }
        }

        using Stream stream = TitleContainer.OpenStream(titleContainerPath);
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
