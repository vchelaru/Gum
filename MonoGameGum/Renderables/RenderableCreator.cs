using Gum.Wireframe;
using RenderingLibrary.Graphics;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Renderables;

/// <summary>
/// Creates the renderable for a standard element base type. Assigned to
/// <c>ElementSaveExtensions.CustomCreateGraphicalComponentFunc</c> when the project loads.
/// </summary>
public static class RenderableCreator
{
    /// <summary>
    /// Returns the renderable for <paramref name="type"/>, or null when the type is not a standard
    /// base type.
    /// </summary>
    public static IRenderable? HandleCreateGraphicalComponent(string type, ISystemManagers systemManagers)
    {
        return FallbackRenderableFactory.TryHandleAsBaseType(type, systemManagers as SystemManagers);
    }
}
