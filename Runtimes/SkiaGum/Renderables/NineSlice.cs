using Gum;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace SkiaGum.Renderables;

public class NineSlice : IRenderable
{
    public BlendState BlendState => BlendState.NonPremultiplied;

    public bool Wrap => false;

    public string BatchKey => string.Empty;

    public void EndBatch(ISystemManagers systemManagers)
    {
    }

    public void PreRender()
    {

    }

    public void Render(ISystemManagers managers)
    {
        throw new System.NotImplementedException("NineSlices are not yet supported in Skia. Please complain on Discord or in a github issue");
    }

    public void StartBatch(ISystemManagers systemManagers)
    {
    }
}
