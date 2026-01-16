using Gum.DataTypes;
using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.GueDeriving;

public interface ISkBitmapLoader
{
    bool TryLoad(string path, out SKBitmap bitmap);
}

/// <summary>
/// Default loader adapter; keeps the global dependency OUTSIDE the runtime.
/// </summary>
public sealed class GumSkBitmapLoader : ISkBitmapLoader
{
    public bool TryLoad(string path, out SKBitmap bitmap)
    {
        try
        {
            var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
            var contentLoader = loaderManager.ContentLoader;
            bitmap = contentLoader.LoadContent<SKBitmap>(path);
            return bitmap != null;
        }
        catch
        {
            bitmap = null!;
            return false;
        }
    }
}

public enum TextureOwnership
{
    /// <summary>Runtime does not dispose textures assigned to it.</summary>
    External,

    /// <summary>Runtime disposes the previous texture when replaced/cleared.</summary>
    OwnedByRuntime
}

public sealed class SpriteRuntime : SkiaRuntime<Sprite>
{
    private readonly ISkBitmapLoader _loader;
    private string? _sourceFile;

    public TextureOwnership Ownership { get; set; } = TextureOwnership.External;

    public string? SourceFile
    {
        get => _sourceFile;
        set => SetSourceFile(value);
    }

    public SKBitmap? Texture
    {
        get => Renderable.Texture;
        set => SetTexture(value);
    }

    public SKImage? Image
    {
        get => Renderable.Image;
        set => Renderable.Image = value;
    }

    public SpriteRuntime(bool fullInstantiation = true, ISkBitmapLoader? loader = null)
        : base(new Sprite(), fullInstantiation)
    {
        _loader = loader ?? new GumSkBitmapLoader();

        if (!fullInstantiation) return;

        WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        Width = 100;
        Height = 100;
    }

    public override GraphicalUiElement Clone()
    {
        var clone = (SpriteRuntime)base.Clone();

        // Keep state consistent in clones:
        clone._sourceFile = _sourceFile;
        clone.Ownership = Ownership;

        // Textures are typically shared/cached by loader; do NOT auto-dispose in clone.
        // If you want deep-clone textures, do it explicitly elsewhere.
        return clone;
    }

    private void SetSourceFile(string? path)
    {
        _sourceFile = string.IsNullOrWhiteSpace(path) ? null : path;

        if (_sourceFile is null)
        {
            SetTexture(null);
            return;
        }

        if (_loader.TryLoad(_sourceFile, out var bitmap))
        {
            SetTexture(bitmap);
        }
        else
        {
            // Failed load: clear texture but keep SourceFile so you can debug/report.
            SetTexture(null);
        }
    }

    private void SetTexture(SKBitmap? bitmap)
    {
        if (Ownership == TextureOwnership.OwnedByRuntime)
        {
            // Dispose previous texture if we own it:
            Renderable.Texture?.Dispose();
            Renderable.Image?.Dispose();
        }

        Renderable.Texture = bitmap;

        // Optional: derive Image lazily if you prefer SKImage pipeline:
        // Renderable.Image = bitmap != null ? SKImage.FromBitmap(bitmap) : null;
        // (If you do that, make sure disposal rules are clear.)
    }
}
