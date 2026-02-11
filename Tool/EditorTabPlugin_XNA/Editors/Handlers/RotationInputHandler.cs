using System;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Color = System.Drawing.Color;
using HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment;
using Gum.Input;


namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// Handles rotation of selected objects via the rotation handle.
/// </summary>
public class RotationInputHandler : InputHandlerBase
{
    private bool _isHighlighted = false;
    private readonly LineCircle _rotationHandle;

    public override int Priority => 100; // Highest priority

    public RotationInputHandler(EditorContext context, LineCircle rotationHandle)
        : base(context)
    {
        _rotationHandle = rotationHandle;
        _rotationHandle.Color = Color.Yellow;
        _rotationHandle.Visible = false;
    }

    public override bool HasCursorOver(float worldX, float worldY)
    {
        return _isHighlighted;
    }

    public override Cursor? GetCursorToShow(float worldX, float worldY)
    {
        // Could return a custom rotation cursor if available
        return _isHighlighted ? System.Windows.Forms.Cursors.Cross : null;
    }

    public override void UpdateHover(float worldX, float worldY)
    {
        _isHighlighted = _rotationHandle.Visible && _rotationHandle.HasCursorOver(worldX, worldY);
        UpdateRotationHandlePosition();
    }

    protected override void OnPush(float worldX, float worldY)
    {
        Context.UpdateAspectRatioForGrabbedIpso();
    }

    protected override void OnDrag()
    {
        if (Context.SelectedObjects.Count == 0) return;

        var gue = Context.SelectedObjects.First();

        var originX = gue.AbsoluteX;
        var originY = gue.AbsoluteY;

        var cursorX = InputLibrary.Cursor.Self.GetWorldX();
        var cursorY = InputLibrary.Cursor.Self.GetWorldY();

        var angleInRadians = (float)Math.Atan2(cursorY - originY, cursorX - originX);
        var rotationValueDegrees = -MathHelper.ToDegrees(angleInRadians);

        // Snap to 15 degrees if hotkey pressed
        if (Context.HotkeyManager.SnapRotationTo15Degrees.IsPressedInControl())
        {
            rotationValueDegrees = MathFunctions.RoundFloat(rotationValueDegrees, 15);
        }

        // Account for parent rotation
        float parentRotation = 0;
        if (gue.Parent != null)
        {
            parentRotation = gue.Parent.GetAbsoluteRotation();
        }

        gue.Rotation = rotationValueDegrees - parentRotation;

        // Update state
        string nameWithInstance = "Rotation";
        if (Context.SelectedState.SelectedInstance != null)
        {
            nameWithInstance = Context.SelectedState.SelectedInstance.Name + "." + nameWithInstance;
        }

        Context.SelectedState.SelectedStateSave.SetValue(
            nameWithInstance,
            rotationValueDegrees - parentRotation,
            Context.SelectedState.SelectedInstance,
            "float");

        Context.VariablePropagationLogic.PropagateVariablesInCategory(
            nameWithInstance,
            Context.SelectedState.SelectedElement,
            Context.SelectedState.SelectedStateCategorySave);

        Context.GuiCommands.RefreshVariableValues();
        MarkAsChanged();
    }

    protected override void OnRelease()
    {
        if (Context.HasChangedAnythingSinceLastPush)
        {
            DoEndOfSettingValuesLogic();
        }
    }

    public override void OnSelectionChanged()
    {
        if (Context.SelectedObjects.Count != 1 || Context.SelectedObjects.Any(item => item.Tag is ScreenSave))
        {
            _rotationHandle.Visible = false;
        }
        else
        {
            _rotationHandle.Visible = true;
            UpdateRotationHandlePosition();
        }
    }

    private void UpdateRotationHandlePosition()
    {
        if (Context.SelectedObjects.Count != 1)
        {
            _rotationHandle.Visible = false;
            return;
        }

        var singleSelectedObject = Context.SelectedObjects[0];
        _rotationHandle.Visible = true;

        float minimumOffset = 24 / Renderer.Self.Camera.Zoom;
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
        _rotationHandle.Radius = 8 / Renderer.Self.Camera.Zoom;
    }

    private void DoEndOfSettingValuesLogic()
    {
        var selectedElement = Context.SelectedState.SelectedElement;
        var stateSave = Context.SelectedState.SelectedStateSave;
        if (stateSave == null)
        {
            throw new InvalidOperationException("The SelectedStateSave is null, this should not happen");
        }

        Context.FileCommands.TryAutoSaveElement(selectedElement);

        using var undoLock = Context.UndoManager.RequestLock();

        Context.GuiCommands.RefreshVariableValues();

        Context.HasChangedAnythingSinceLastPush = false;
    }

    public void Destroy()
    {
        ShapeManager.Self.Remove(_rotationHandle);
    }
}
