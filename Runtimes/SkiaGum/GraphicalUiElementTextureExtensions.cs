using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp;

namespace Gum;

/// <summary>
/// Skia copies a bitmap's pixels into an image when it is assigned to a Sprite or NineSlice <c>Texture</c>, and has no
/// signal when the bitmap is edited afterward. These calls copy the pixels again.
/// </summary>
public static class GraphicalUiElementTextureExtensions
{
    /// <summary>
    /// Copies the current pixels of the <c>Texture</c> bitmap into every Sprite and NineSlice on this element and
    /// its children, so edits written since the bitmap was assigned show. Elements without a texture, or whose
    /// bitmap was disposed, are skipped.
    /// </summary>
    /// <param name="element">The element to start from.</param>
    public static void RefreshTextures(this GraphicalUiElement element) => Refresh(element, null);

    /// <summary>
    /// Like <see cref="RefreshTextures(GraphicalUiElement)"/>, but only for Sprites and NineSlices whose
    /// <c>Texture</c> is <paramref name="bitmap"/>.
    /// </summary>
    /// <param name="element">The element to start from.</param>
    /// <param name="bitmap">The bitmap that was edited.</param>
    public static void RefreshTextures(this GraphicalUiElement element, SKBitmap bitmap) => Refresh(element, bitmap);

    private static void Refresh(GraphicalUiElement element, SKBitmap? onlyBitmap)
    {
        switch (element.RenderableComponent)
        {
            case Sprite sprite:
                sprite.RefreshTexture(onlyBitmap);
                break;
            case NineSlice nineSlice:
                nineSlice.RefreshTexture(onlyBitmap);
                break;
        }

        for (int i = 0; i < element.Children.Count; i++)
        {
            Refresh(element.Children[i], onlyBitmap);
        }
    }
}
