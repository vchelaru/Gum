using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System.Drawing;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Creates the editor canvas's renderable for a standard element base type. Defers to the runtime's
/// <see cref="FallbackRenderableFactory"/>, then applies the editor-only look: a Container's outline
/// is dotted and painted the project's outline color (issue #4849).
/// </summary>
public class EditorRenderableFactory
{
    private readonly IProjectState _projectState;

    public EditorRenderableFactory(IProjectState projectState)
    {
        _projectState = projectState;
    }

    public IRenderableIpso? CreateRenderableForType(string type, ISystemManagers? managers)
    {
        IRenderable? renderable = FallbackRenderableFactory.TryHandleAsBaseType(type, managers);

        // The fallback returns a LineRectangle for "Rectangle" too; only a Container outline is dotted.
        if (type is "Container" or "Component" && renderable is LineRectangle outline)
        {
            outline.IsDotted = true;
            outline.Color = Color.FromArgb(
                255,
                _projectState.OutlineColorR,
                _projectState.OutlineColorG,
                _projectState.OutlineColorB);
        }

        return renderable as IRenderableIpso;
    }
}
