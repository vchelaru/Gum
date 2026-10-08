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

    public bool IntersectsRectangle(GraphicalUiElement element, float left, float top, float right, float bottom)
    {
        if (element.RenderableComponent is LinePolygon linePolygon)
        {
            return linePolygon.IntersectsRectangle(left, top, right, bottom);
        }

        return !(element.GetAbsoluteRight() < left ||
                 element.GetAbsoluteLeft() > right ||
                 element.GetAbsoluteBottom() < top ||
                 element.GetAbsoluteTop() > bottom);
    }
}
