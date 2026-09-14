using Gum.DataTypes;
using RenderingLibrary.Graphics;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Visual component that wraps ResizeHandles and manages their update logic.
/// Provides the 8 resize handles that appear on selected objects.
/// </summary>
public class ResizeHandlesVisual : EditorVisualBase, IResizeHandlesVisual
{
    private readonly ResizeHandles _resizeHandles;

    /// <summary>
    /// Gets the underlying resize handles for input handling.
    /// </summary>
    public ResizeHandles Handles => _resizeHandles;

    // IResizeHandlesVisual — the narrow, headless-safe surface ResizeInputHandler (now in
    // Gum.Presentation) needs. ResizeHandles itself can't be exposed there: it draws
    // LineRectangles, which are XNALIKE-only.
    float IResizeHandlesVisual.HandlesWidth => _resizeHandles.Width;
    float IResizeHandlesVisual.HandlesHeight => _resizeHandles.Height;
    ResizeSide IResizeHandlesVisual.GetSideOver(float worldX, float worldY) => _resizeHandles.GetSideOver(worldX, worldY);

    public bool ShowOrigin
    {
        get => _resizeHandles.ShowOrigin;
        set => _resizeHandles.ShowOrigin = value;
    }

    public ResizeHandlesVisual(EditorContext context, Color lineColor) : base(context)
    {
        _resizeHandles = new ResizeHandles(OverlayLayer, lineColor);
        _resizeHandles.ShowOrigin = true;
        Visible = false; // Sync base visibility with handles
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _resizeHandles.Visible = isVisible;
    }

    public override void Update()
    {
        if (!Visible || Context.SelectedObjects.Count == 0) return;

        _resizeHandles.SetValuesFrom(Context.SelectedObjects);
        _resizeHandles.UpdateHandleSizes();
    }

    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        if (selectedObjects.Count ==0 || selectedObjects.Any(item => item.Tag is ScreenSave))
        {
            Visible = false;
            return;
        }

        if (Context.IsSelectionLocked() || (!Context.IsWidthChangeEnabled && !Context.IsHeightChangeEnabled))
        {
            Visible = false;
            return;
        }

        Visible = true;
        _resizeHandles.SetValuesFrom(selectedObjects);
        _resizeHandles.UpdateHandleSizes();
    }

    public override void Destroy()
    {
        _resizeHandles.Destroy();
    }
}
