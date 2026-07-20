using System;
using System.Linq;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Wireframe.Editors.Visuals;
using RenderingLibrary;
using RenderingLibrary.Math;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// Handles rotation of selected objects via the rotation handle.
/// </summary>
public class RotationInputHandler : InputHandlerBase
{
    private bool _isHighlighted = false;
    private readonly IRotationHandleVisual _rotationHandleVisual;

    public override int Priority => 100; // Highest priority

    public RotationInputHandler(EditorContext context, IRotationHandleVisual rotationHandleVisual)
        : base(context)
    {
        _rotationHandleVisual = rotationHandleVisual;
    }

    public override bool HasCursorOver(float worldX, float worldY)
    {
        return Context.IsRotationEnabled && _rotationHandleVisual.HandleVisible && _rotationHandleVisual.HandleHasCursorOver(worldX, worldY);
    }

    public override GumCursorKind? GetCursorToShow(float worldX, float worldY)
    {
        return _isHighlighted ? GumCursorKind.Hand : null;
    }

    public override void UpdateHover(float worldX, float worldY)
    {
        _isHighlighted = Context.IsRotationEnabled && _rotationHandleVisual.HandleVisible && _rotationHandleVisual.HandleHasCursorOver(worldX, worldY);
    }

    protected override void OnPush(float worldX, float worldY)
    {
        Context.UpdateAspectRatioForGrabbedIpso();
    }

    protected override void OnDrag()
    {
        if (Context.SelectedObjects.Count == 0) return;
        if (!Context.IsRotationEnabled) return;

        var gue = Context.SelectedObjects.First();

        var originX = gue.AbsoluteX;
        var originY = gue.AbsoluteY;

        var cursor = Context.Cursor;
        Context.Camera.ScreenToWorld(cursor.X, cursor.Y, out float cursorX, out float cursorY);

        var angleInRadians = (float)Math.Atan2(cursorY - originY, cursorX - originX);
        var rotationValueDegrees = -MathHelper.ToDegrees(angleInRadians);

        // Snap to 15 degrees if hotkey pressed
        if (Context.HotkeyManager.IsPressedInControl(Context.HotkeyManager.SnapRotationTo15Degrees))
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
