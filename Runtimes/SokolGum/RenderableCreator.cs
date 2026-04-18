using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Gum.Renderables;

namespace SokolGum;

/// <summary>
/// Maps Gum element base-type names to the sokol-backed
/// <see cref="IRenderable"/> that sits inside a GraphicalUiElement.
/// Wired to Gum via <c>ElementSaveExtensions.CustomCreateGraphicalComponentFunc</c>
/// from <see cref="SystemManagers.Initialize"/>.
///
/// The runtime wrapper (e.g. <see cref="GueDeriving.ColoredRectangleRuntime"/>)
/// is already picked by <c>ElementSaveExtensions.RegisterGueInstantiation</c>
/// — this callback supplies the inner renderable when no registered runtime
/// handles the type on its own.
/// </summary>
public static class RenderableCreator
{
    public static IRenderable? HandleCreateGraphicalComponent(string type, ISystemManagers? managers) => type switch
    {
        "Container" or "Component" => new InvisibleRenderable(),
        "ColoredRectangle"         => new SolidRectangle(),
        "Sprite"                   => new Sprite(),
        "NineSlice"                => new NineSlice(),
        "Text"                     => new Text(),
        "Rectangle"                => new LineRectangle { IsDotted = false },
        "Circle"                   => new LineCircle    { CircleOrigin = CircleOrigin.TopLeft },
        "Polygon"                  => new LinePolygon(),
        _                          => null,
    };
}
