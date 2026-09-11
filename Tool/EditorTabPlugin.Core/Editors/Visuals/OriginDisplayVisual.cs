using RenderingLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Visual component that displays the origin marker and the line
/// connecting from the object's origin to its parent's origin point.
/// Used primarily for polygon editing.
/// </summary>
public class OriginDisplayVisual : EditorVisualBase
{
    private readonly OriginDisplay _originDisplay;

    public OriginDisplayVisual(EditorContext context) : base(context)
    {
        _originDisplay = new OriginDisplay(OverlayLayer);
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _originDisplay.Visible = isVisible;
    }

    public override void Update()
    {
        if (!Visible || Context.SelectedObjects.Count == 0) return;

        _originDisplay.UpdateTo(Context.SelectedObjects.First());
    }

    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        if (selectedObjects.Count == 0)
        {
            Visible = false;
            return;
        }

        Visible = true;
        _originDisplay.UpdateTo(selectedObjects.First());
    }

    /// <summary>
    /// Set the color of the origin display lines.
    /// </summary>
    public void SetColor(System.Drawing.Color color)
    {
        _originDisplay.SetColor(color);
    }

    public override void Destroy()
    {
        _originDisplay.Destroy();
    }
}
