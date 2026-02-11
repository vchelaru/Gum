using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Handlers;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using ToolsUtilitiesStandard.Helpers;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Visual component that displays dimension measurements (width or height)
/// with endcaps and text when hovering over resize handles.
/// </summary>
public class DimensionDisplayVisual : EditorVisualBase
{
    #region Fields

    private readonly WidthOrHeight _dimensionType;
    private readonly Line _endCap1;
    private readonly Line _endCap2;
    private readonly Line _middleLine;
    private readonly Text _dimensionDisplayText;
    private readonly IUiSettingsService _uiSettingsService;
    private readonly ResizeInputHandler _resizeInputHandler;

    #endregion

    public DimensionDisplayVisual(EditorContext context, WidthOrHeight dimensionType, ResizeInputHandler resizeInputHandler) : base(context)
    {
        _dimensionType = dimensionType;
        _resizeInputHandler = resizeInputHandler;
        _uiSettingsService = Locator.GetRequiredService<IUiSettingsService>();

        var systemManagers = SystemManagers.Default;
        var toolFontService = ToolFontService.Self;

        // Initialize middle line
        _middleLine = new Line(systemManagers);
        _middleLine.Name = "MiddleLine";
        systemManagers.ShapeManager.Add(_middleLine, OverlayLayer);

        // Initialize dimension display text
        _dimensionDisplayText = new Text(systemManagers);
        _dimensionDisplayText.RenderBoundary = false;
        _dimensionDisplayText.Width = null;
        _dimensionDisplayText.Height = 0;
        _dimensionDisplayText.Name = "Dimension display text";
        _dimensionDisplayText.BitmapFont = toolFontService.ToolFont;
        systemManagers.TextManager.Add(_dimensionDisplayText, OverlayLayer);

        // Initialize end caps
        _endCap1 = new Line(systemManagers);
        _endCap1.Name = "EndCap 1";
        systemManagers.ShapeManager.Add(_endCap1, OverlayLayer);

        _endCap2 = new Line(systemManagers);
        _endCap2.Name = "EndCap 2";
        systemManagers.ShapeManager.Add(_endCap2, OverlayLayer);

        // Set colors
        SetColor(context.LineColor, context.TextColor);
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _endCap1.Visible = isVisible;
        _endCap2.Visible = isVisible;
        _middleLine.Visible = isVisible;
        _dimensionDisplayText.Visible = isVisible;
    }

    public override void Update()
    {
        if (Context.SelectedObjects.Count == 0)
        {
            Visible = false;
            return;
        }

        // Determine visibility based on which resize side is hovered
        var sideOver = _resizeInputHandler.SideOver;
        bool shouldBeVisible = ShouldShowForSide(sideOver);

        Visible = shouldBeVisible;

        if (shouldBeVisible)
        {
            UpdateDimensionDisplay(Context.SelectedObjects.First());
        }
    }

    private bool ShouldShowForSide(ResizeSide sideOver)
    {
        if (_dimensionType == WidthOrHeight.Width)
        {
            return sideOver == ResizeSide.TopLeft ||
                   sideOver == ResizeSide.Left ||
                   sideOver == ResizeSide.BottomLeft ||
                   sideOver == ResizeSide.TopRight ||
                   sideOver == ResizeSide.Right ||
                   sideOver == ResizeSide.BottomRight;
        }
        else // Height
        {
            return sideOver == ResizeSide.TopLeft ||
                   sideOver == ResizeSide.Top ||
                   sideOver == ResizeSide.TopRight ||
                   sideOver == ResizeSide.BottomLeft ||
                   sideOver == ResizeSide.Bottom ||
                   sideOver == ResizeSide.BottomRight;
        }
    }

    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        if (selectedObjects.Count == 0)
        {
            Visible = false;
            return;
        }

        // The visibility will be controlled externally based on hover state,
        // so we don't set Visible = true here
        UpdateDimensionDisplay(selectedObjects.First());
    }

    /// <summary>
    /// Set the color of the dimension display lines and text.
    /// </summary>
    public void SetColor(Color lineColor, Color textColor)
    {
        _endCap1.Color = lineColor;
        _endCap2.Color = lineColor;
        _middleLine.Color = lineColor;
        _dimensionDisplayText.Color = textColor;
    }

    public override void Destroy()
    {
        var systemManagers = SystemManagers.Default;
        systemManagers.ShapeManager.Remove(_endCap1);
        systemManagers.ShapeManager.Remove(_endCap2);
        systemManagers.ShapeManager.Remove(_middleLine);
        systemManagers.TextManager.Remove(_dimensionDisplayText);
    }

    #region Update Logic

    private void UpdateDimensionDisplay(GraphicalUiElement objectToUpdateTo)
    {
        var asIpso = objectToUpdateTo as IRenderableIpso;

        var left = asIpso.GetAbsoluteX();
        var absoluteWidth = asIpso.Width;

        var top = asIpso.GetAbsoluteY();
        var absoluteHeight = asIpso.Height;

        var topLeft = new Vector2(left, top);

        // Adjust the Font size based on the UI's scale. Instead of the Editors zoom level.
        // This should have ZERO affect for anyone not using UI level zoom
        float scaleFactor = (float)(_uiSettingsService.BaseFontSize / 12);
        float decreasedScaleFactor = scaleFactor * 0.75f;
        float finalizedScaleFactor = decreasedScaleFactor < 1 ? 1 : decreasedScaleFactor;

        // The dividing by zoom makes sure when we zoom the editor, it retains the same size for text
        // even though the editor objects are changing in size.
        float adjustedScaleFactorWithEditorZoom = finalizedScaleFactor / Zoom;

        // Apply zoom factors combined to the offsets and font scale
        float fromBodyOffset = 26 * adjustedScaleFactorWithEditorZoom;
        float endCapLength = 12 * adjustedScaleFactorWithEditorZoom;
        _dimensionDisplayText.FontScale = adjustedScaleFactorWithEditorZoom;

        var rotationMatrix = asIpso.GetAbsoluteRotationMatrix();
        var rotatedLeftDirection = new Vector2(rotationMatrix.Left().X, rotationMatrix.Left().Y);
        var rotatedRightDirection = new Vector2(rotationMatrix.Right().X, rotationMatrix.Right().Y);
        var rotatedUpDirection = new Vector2(rotationMatrix.Down().X, rotationMatrix.Down().Y);
        var rotatedDownDirection = new Vector2(rotationMatrix.Up().X, rotationMatrix.Up().Y);
        var extraTextOffset = 4;

        if (_dimensionType == WidthOrHeight.Width)
        {
            UpdateWidthDisplay(objectToUpdateTo, topLeft, absoluteWidth, fromBodyOffset,
                endCapLength, extraTextOffset, rotatedUpDirection, rotatedRightDirection,
                rotatedDownDirection);
        }
        else // height
        {
            UpdateHeightDisplay(objectToUpdateTo, topLeft, absoluteHeight, fromBodyOffset,
                endCapLength, extraTextOffset, rotatedLeftDirection, rotatedRightDirection,
                rotatedDownDirection);
        }
    }

    private void UpdateWidthDisplay(GraphicalUiElement objectToUpdateTo, Vector2 topLeft,
        float absoluteWidth, float fromBodyOffset, float endCapLength, float extraTextOffset,
        Vector2 rotatedUpDirection, Vector2 rotatedRightDirection, Vector2 rotatedDownDirection)
    {
        string suffix = GetDimensionSuffix(objectToUpdateTo.WidthUnits, isWidth: true);

        _middleLine.SetPosition(topLeft + rotatedUpDirection * fromBodyOffset);
        _middleLine.RelativePoint = rotatedRightDirection * absoluteWidth;

        if (suffix != null)
        {
            _dimensionDisplayText.RawText = $"{objectToUpdateTo.Width:0.0}{suffix}\n({absoluteWidth:0.0} px)";
        }
        else
        {
            _dimensionDisplayText.RawText = absoluteWidth.ToString("0.0");
        }

        var desiredPosition =
            topLeft +
            rotatedUpDirection * (fromBodyOffset + extraTextOffset + _dimensionDisplayText.WrappedTextHeight) +
            rotatedRightDirection * (-_dimensionDisplayText.WrappedTextWidth / 2 + absoluteWidth / 2);

        desiredPosition = ClampPositionToCamera(desiredPosition);

        _dimensionDisplayText.SetPosition(desiredPosition);
        _dimensionDisplayText.HorizontalAlignment = HorizontalAlignment.Center;
        _dimensionDisplayText.VerticalAlignment = VerticalAlignment.Center;

        _endCap1.SetPosition(_middleLine.GetPosition() + rotatedUpDirection * endCapLength / 2.0f);
        _endCap1.RelativePoint = rotatedDownDirection * endCapLength;

        _endCap2.SetPosition(_middleLine.GetPosition() + rotatedRightDirection * absoluteWidth + rotatedUpDirection * endCapLength / 2.0f);
        _endCap2.RelativePoint = rotatedDownDirection * endCapLength;
    }

    private void UpdateHeightDisplay(GraphicalUiElement objectToUpdateTo, Vector2 topLeft,
        float absoluteHeight, float fromBodyOffset, float endCapLength, float extraTextOffset,
        Vector2 rotatedLeftDirection, Vector2 rotatedRightDirection, Vector2 rotatedDownDirection)
    {
        string suffix = GetDimensionSuffix(objectToUpdateTo.HeightUnits, isWidth: false);

        // up is 0,1,0, which is actually down for Gum. Confusing, I know, but this results in the correct math
        _middleLine.X = (topLeft + rotatedLeftDirection * fromBodyOffset).X;
        _middleLine.Y = (topLeft + rotatedLeftDirection * fromBodyOffset).Y;

        _middleLine.RelativePoint = rotatedDownDirection * absoluteHeight;

        if (suffix != null)
        {
            _dimensionDisplayText.RawText = $"{objectToUpdateTo.Height:0.0}{suffix}\n({absoluteHeight:0.0} px)";
        }
        else
        {
            _dimensionDisplayText.RawText = absoluteHeight.ToString("0.0");
        }

        var desiredPosition = topLeft + rotatedDownDirection * .5f * absoluteHeight + rotatedLeftDirection * (fromBodyOffset + extraTextOffset);

        desiredPosition.X = desiredPosition.X - _dimensionDisplayText.WrappedTextWidth;
        desiredPosition.Y = desiredPosition.Y - _dimensionDisplayText.WrappedTextHeight / 2.0f;

        desiredPosition = ClampPositionToCamera(desiredPosition);

        _dimensionDisplayText.Position = desiredPosition;
        _dimensionDisplayText.VerticalAlignment = VerticalAlignment.Center;

        _endCap1.SetPosition(_middleLine.GetPosition() + rotatedLeftDirection * endCapLength / 2.0f);
        _endCap1.RelativePoint = rotatedRightDirection * endCapLength;

        _endCap2.SetPosition(_middleLine.GetPosition() + _middleLine.RelativePoint + rotatedLeftDirection * endCapLength / 2.0f);
        _endCap2.RelativePoint = rotatedRightDirection * endCapLength;
    }

    private string GetDimensionSuffix(DimensionUnitType unitType, bool isWidth)
    {
        return unitType switch
        {
            DimensionUnitType.MaintainFileAspectRatio => " File Aspect Ratio",
            DimensionUnitType.PercentageOfParent => "% Parent",
            DimensionUnitType.PercentageOfOtherDimension => isWidth ? "% Height" : "% Width",
            DimensionUnitType.PercentageOfSourceFile => "% File",
            DimensionUnitType.Ratio => " Ratio of Parent",
            DimensionUnitType.RelativeToChildren => " Relative to Children",
            DimensionUnitType.RelativeToParent => " Relative to Parent",
            _ => null
        };
    }

    private Vector2 ClampPositionToCamera(Vector2 desiredPosition)
    {
        var camera = Renderer.Self.Camera;

        if (desiredPosition.X + _dimensionDisplayText.WrappedTextWidth > camera.AbsoluteRight)
        {
            desiredPosition.X = camera.AbsoluteRight - _dimensionDisplayText.WrappedTextWidth;
        }

        const float rulerPadding = 12;

        if (desiredPosition.X < camera.AbsoluteLeft + rulerPadding)
        {
            desiredPosition.X = camera.AbsoluteLeft + rulerPadding;
        }

        if (desiredPosition.Y < camera.AbsoluteTop + rulerPadding)
        {
            desiredPosition.Y = camera.AbsoluteTop + rulerPadding;
        }
        if (desiredPosition.Y + _dimensionDisplayText.WrappedTextHeight > camera.AbsoluteBottom)
        {
            desiredPosition.Y = camera.AbsoluteBottom - _dimensionDisplayText.WrappedTextHeight;
        }

        return desiredPosition;
    }

    #endregion
}
