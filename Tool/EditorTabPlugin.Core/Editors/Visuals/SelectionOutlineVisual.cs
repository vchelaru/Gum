using EditorTabPlugin_XNA.Utilities;
using Gum.DataTypes;
using RenderingLibrary;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Outline around the full bounds of a selected instance, shown when the resize handles
/// can't tell the user where the selection is. Draws in whatever the <c>shouldShow</c>
/// predicate selects; <see cref="LockedSelectionVisual"/> and <see cref="HiddenSizeSelectionVisual"/>
/// are the two uses.
/// </summary>
public class SelectionOutlineVisual : EditorVisualBase
{
    private readonly LineRectangle _outline;
    private readonly Func<ICollection<GraphicalUiElement>, bool> _shouldShow;

    public SelectionOutlineVisual(
        EditorContext context,
        Color lineColor,
        bool isDotted,
        Func<ICollection<GraphicalUiElement>, bool> shouldShow) : base(context)
    {
        _shouldShow = shouldShow;
        _outline = new LineRectangle();
        _outline.IsDotted = isDotted;
        _outline.Color = lineColor;
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
        if (!_shouldShow(selectedObjects))
        {
            Visible = false;
            return;
        }

        Visible = true;
        UpdateOutlineBounds(selectedObjects.First());
    }

    private void UpdateOutlineBounds(GraphicalUiElement selected)
    {
        var border = ToWorldOverlaySize(1);
        var bounds = selected.GetBounds();
        _outline.X = bounds.left - border;
        _outline.Y = bounds.top - border;
        _outline.Width = bounds.right - bounds.left + border * 2;
        _outline.Height = bounds.bottom - bounds.top + border * 2;
        _outline.Rotation = selected.GetAbsoluteRotation();
        _outline.LinePixelWidth = Context.DisplayScale.DisplayScale;
    }

    public override void Destroy()
    {
        ShapeManager.Self.Remove(_outline);
    }
}

/// <summary>
/// Displays a dashed outline around a locked selected instance.
/// Shown instead of resize handles when the selection is locked, so the user can
/// always see where the selected object is in the canvas regardless of its Visible property.
/// </summary>
public class LockedSelectionVisual : SelectionOutlineVisual
{
    public LockedSelectionVisual(EditorContext context, Color lineColor)
        : base(context, lineColor, isDotted: true, selectedObjects =>
            selectedObjects.Count == 1 && (selectedObjects.First().Tag as InstanceSave)?.Locked == true)
    {
    }
}

/// <summary>
/// Solid outline, in the resize handle color, around a selected instance whose component hides
/// Width or Height. See <see cref="SelectionOutlineRule"/>.
/// </summary>
public class HiddenSizeSelectionVisual : SelectionOutlineVisual
{
    public HiddenSizeSelectionVisual(EditorContext context, Color lineColor, Gum.Managers.IObjectFinder objectFinder)
        : base(context, lineColor, isDotted: false, selectedObjects =>
            SelectionOutlineRule.ShouldShow(selectedObjects.Select(item => item.Tag).ToList(), objectFinder))
    {
    }
}
