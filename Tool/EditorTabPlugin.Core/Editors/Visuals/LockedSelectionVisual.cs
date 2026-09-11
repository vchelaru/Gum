using EditorTabPlugin_XNA.Utilities;
using Gum.DataTypes;
using RenderingLibrary;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Visual component that displays a dashed outline around a locked selected instance.
/// Shown instead of resize handles when the selection is locked, so the user can
/// always see where the selected object is in the canvas regardless of its Visible property.
/// </summary>
public class LockedSelectionVisual : EditorVisualBase
{
    private readonly LineRectangle _outline;

    public LockedSelectionVisual(EditorContext context, Color lineColor) : base(context)
    {
        _outline = new LineRectangle();
        _outline.IsDotted = true;
        _outline.Color = lineColor;
        _outline.LinePixelWidth = 1;
        ShapeManager.Self.Add(_outline, OverlayLayer);
        Visible = false; // Sync _visible in base class; must come after _outline is assigned
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _outline.Visible = isVisible;
    }

    public override void Update()
    {
        if (!Visible || Context.SelectedObjects.Count == 0) return;

        UpdateOutlineBounds(Context.SelectedObjects.First());
    }

    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        var isSingleLockedInstance = selectedObjects.Count == 1 &&
            (selectedObjects.First().Tag as InstanceSave)?.Locked == true;

        if (!isSingleLockedInstance)
        {
            Visible = false;
            return;
        }

        Visible = true;
        UpdateOutlineBounds(selectedObjects.First());
    }

    private void UpdateOutlineBounds(GraphicalUiElement selected)
    {
        var border = ScaleByZoom(1);
        var bounds = selected.GetBounds();
        _outline.X = bounds.left - border;
        _outline.Y = bounds.top - border;
        _outline.Width = bounds.right - bounds.left + border * 2;
        _outline.Height = bounds.bottom - bounds.top + border * 2;
        _outline.Rotation = selected.GetAbsoluteRotation();
    }

    public override void Destroy()
    {
        ShapeManager.Self.Remove(_outline);
    }
}
