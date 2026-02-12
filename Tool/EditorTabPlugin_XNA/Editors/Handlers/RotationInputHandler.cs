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
using Gum.Wireframe.Editors.Visuals;


namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// Handles rotation of selected objects via the rotation handle.
/// </summary>
public class RotationInputHandler : InputHandlerBase
{
    private bool _isHighlighted = false;
    private readonly RotationHandleVisual _rotationHandleVisual;

    public override int Priority => 100; // Highest priority

    public RotationInputHandler(EditorContext context, RotationHandleVisual rotationHandleVisual)
        : base(context)
    {
        _rotationHandleVisual = rotationHandleVisual;
    }

    public override bool HasCursorOver(float worldX, float worldY)
    {
        return _rotationHandleVisual.Handle.Visible && _rotationHandleVisual.Handle.HasCursorOver(worldX, worldY);
    }

    public override Cursor? GetCursorToShow(float worldX, float worldY)
    {
        return _isHighlighted ? System.Windows.Forms.Cursors.Hand : null;
    }

    public override void UpdateHover(float worldX, float worldY)
    {
        _isHighlighted = _rotationHandleVisual.Handle.Visible && _rotationHandleVisual.Handle.HasCursorOver(worldX, worldY);
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
        // Visual handles its own visibility and position updates
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
        // Visual handles its own cleanup
    }
}
