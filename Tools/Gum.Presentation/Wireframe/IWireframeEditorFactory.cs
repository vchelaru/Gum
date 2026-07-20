using RenderingLibrary;
using RenderingLibrary.Graphics;
using Gum.Input;

namespace Gum.Wireframe;

/// <summary>
/// Builds the concrete <see cref="WireframeEditor"/> implementation (<c>StandardWireframeEditor</c>
/// or <c>PolygonWireframeEditor</c>) for the current selection. <c>SelectionManager</c> only decides
/// *when* a new editor is needed (polygon vs. standard, or none) — the concrete editors are tool-side
/// (XNALIKE rendering, a tool-only font service, project-settings colors), so building one is pushed
/// behind this seam instead of <c>SelectionManager</c> constructing them directly.
/// </summary>
public interface IWireframeEditorFactory
{
    WireframeEditor CreateStandardEditor(ISelectionManager selectionManager, Layer layer, Camera camera, IGumCursorState cursor);

    WireframeEditor CreatePolygonEditor(ISelectionManager selectionManager, Layer layer, Camera camera, IGumCursorState cursor);
}
