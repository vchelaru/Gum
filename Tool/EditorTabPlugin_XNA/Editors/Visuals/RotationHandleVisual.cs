using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Visual component that displays the rotation handle as a yellow circle
/// positioned to the right of the selected object.
/// </summary>
public class RotationHandleVisual : EditorVisualBase
{
    private readonly LineCircle _rotationHandle;

    private const float MinimumOffsetAtNoZoom = 24;
    private const float RadiusAtNoZoom = 8;

    /// <summary>
    /// Gets the underlying rotation handle shape for input handling.
    /// </summary>
    public LineCircle Handle => _rotationHandle;

    public RotationHandleVisual(EditorContext context, Color color) : base(context)
    {
        _rotationHandle = new LineCircle();
        _rotationHandle.Color = color;
        _rotationHandle.Visible = false;
        ShapeManager.Self.Add(_rotationHandle, OverlayLayer);
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _rotationHandle.Visible = isVisible && Context.SelectedObjects.Count == 1;
    }

    public override void Update()
    {
        if (!Visible || Context.SelectedObjects.Count != 1) return;

        UpdateRotationHandlePosition();
    }

    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        if (selectedObjects.Count != 1 || selectedObjects.Any(item => item.Tag is Gum.DataTypes.ScreenSave))
        {
            Visible = false;
            return;
        }

        Visible = true;
        UpdateRotationHandlePosition();
    }

    private void UpdateRotationHandlePosition()
    {
        if (Context.SelectedObjects.Count != 1)
        {
            _rotationHandle.Visible = false;
            return;
        }

        var singleSelectedObject = Context.SelectedObjects[0];
        _rotationHandle.Visible = Visible;

        float minimumOffset = ScaleByZoom(MinimumOffsetAtNoZoom);
        float xOffset = 0;

        if (singleSelectedObject.XOrigin == HorizontalAlignment.Left)
        {
            xOffset = singleSelectedObject.GetAbsoluteWidth() + minimumOffset;
        }
        else if (singleSelectedObject.XOrigin == HorizontalAlignment.Center)
        {
            xOffset = singleSelectedObject.GetAbsoluteWidth() / 2.0f + minimumOffset;
        }
        else if (singleSelectedObject.XOrigin == HorizontalAlignment.Right)
        {
            xOffset = minimumOffset;
        }

        var offset = new Vector2(xOffset, 0);
        MathFunctions.RotateVector(
            ref offset,
            -MathHelper.ToRadians(singleSelectedObject.GetAbsoluteRotation()));

        _rotationHandle.X = singleSelectedObject.AbsoluteX + offset.X;
        _rotationHandle.Y = singleSelectedObject.AbsoluteY + offset.Y;
        _rotationHandle.Radius = ScaleByZoom(RadiusAtNoZoom);
    }

    public override void Destroy()
    {
        ShapeManager.Self.Remove(_rotationHandle);
    }
}
