using RenderingLibrary;
using RenderingLibrary.Math.Geometry;

namespace Gum.Wireframe;

/// <inheritdoc cref="IPreciseHitTester"/>
public class PreciseHitTester : IPreciseHitTester
{
    public bool HasCursorOver(GraphicalUiElement element, float x, float y)
    {
        if (element.RenderableComponent is LinePolygon linePolygon)
        {
            return linePolygon.IsPointInside(x, y);
        }

        return element.HasCursorOver(x, y);
    }
}
