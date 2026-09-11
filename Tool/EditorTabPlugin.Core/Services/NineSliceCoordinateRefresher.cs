using RenderingLibrary.Graphics;

namespace Gum.Wireframe;

/// <inheritdoc cref="INineSliceCoordinateRefresher"/>
public class NineSliceCoordinateRefresher : INineSliceCoordinateRefresher
{
    public void RefreshIfNineSlice(IRenderable? renderableComponent)
    {
        // This function updates the sizes and texture coordinates of the highlighted
        // representation if it's a NineSlice. This is needed before we set the HighlightedIpso and
        // before we update the highlight objects.
        if (renderableComponent is NineSlice nineSlice)
        {
            nineSlice.RefreshTextureCoordinatesAndSpriteSizes();
        }
    }
}
