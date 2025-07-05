using Gum;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace SkiaGum.Renderables;

public class NineSlice : IRenderable
{
    public BlendState BlendState => BlendState.NonPremultiplied;

    public bool Wrap => false;

    public void PreRender()
    {

    }

    public void Render(ISystemManagers managers)
    {
        throw new System.NotImplementedException("NineSlices are not yet supported in Skia. Please complain on Discord or in a github issue");
    }
}
