using System.Collections.Generic;
using Gum.Wireframe;
using KernSmith.Gum;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace GumPreview;

/// <summary>
/// Bakes a Text's font in memory when the tool has not written its FontCache file yet (#7315).
/// Without it the Preview only reads files, so a project whose Text uses a font file (every Forms
/// theme) drew in the default sans until the tool's bake landed. A font that is already on disk is
/// left to the disk load, because the lookup cascade tries the creator before the disk and the
/// tool's own file (possibly baked by bmfont.exe) is what the editor canvas shows.
/// </summary>
internal sealed class PreviewFontCreator : IInMemoryFontCreator, IGrowableFontCreator, IPremultipliableFontCreator
{
    private readonly KernSmithFontCreator _inner;

    static PreviewFontCreator()
    {
        // The FreeType rasterizer registers itself from a module initializer, which only runs when
        // something loads the assembly. Nothing here names it, so the Native AOT build links it out and
        // every bake fails with "backend 'FreeType' is not registered". Naming a type forces it in.
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(
            typeof(KernSmith.Rasterizers.FreeType.FreeTypeRasterizer).Module.ModuleHandle);
    }

    public PreviewFontCreator(GraphicsDevice graphicsDevice)
    {
        _inner = new KernSmithFontCreator(graphicsDevice);
    }

    public bool PremultiplyAlpha
    {
        get => _inner.PremultiplyAlpha;
        set => _inner.PremultiplyAlpha = value;
    }

    public static void Register(GraphicsDevice graphicsDevice)
    {
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new PreviewFontCreator(graphicsDevice);
    }

    public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
    {
        string cachePath = FileManager.Standardize(bmfcSave.FontCacheFileName, preserveCase: true, makeAbsolute: true);
        if (FileManager.FileExists(cachePath))
        {
            return null;
        }

        return _inner.TryCreateFont(bmfcSave);
    }

    public IReadOnlyList<char>? TryAddGlyphs(BitmapFont font, BmfcSave bmfcSave, string characters)
    {
        return ((IGrowableFontCreator)_inner).TryAddGlyphs(font, bmfcSave, characters);
    }
}
