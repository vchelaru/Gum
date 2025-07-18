using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using ToolsUtilitiesStandard.Helpers;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;
using Gum.Managers;
using Gum.Commands;
using Gum.Services;

namespace Gum.Wireframe.Editors
{
    #region Enums

    public enum WidthOrHeight
    {
        Width,
        Height
    }

    #endregion

    class DimensionDisplay
    {
        #region Fields/Properties

        Line endCap1;
        Line endCap2;

        Line middleLine;
        Text dimensionDisplayText;

        float Zoom => systemManagers.Renderer.Camera.Zoom;

        SystemManagers systemManagers;

        ToolFontService _toolFontService;

        private readonly GuiCommands _guiCommands;

        #endregion

        public DimensionDisplay()
        {
            _toolFontService = ToolFontService.Self;
            _guiCommands = Locator.GetRequiredService<GuiCommands>();
        }

        public void AddToManagers(SystemManagers systemManagers, Layer layer)
        {
            void AddLineToManagers(Line line) => systemManagers.ShapeManager.Add(line, layer);

            middleLine = new Line(systemManagers);
            middleLine.Name = "MiddleLine";
            AddLineToManagers(middleLine);

            dimensionDisplayText = new Text(systemManagers);
            dimensionDisplayText.RenderBoundary = false;
            dimensionDisplayText.Width = null; 
            dimensionDisplayText.Height = 0;
            dimensionDisplayText.Name = "Dimension display text";
            dimensionDisplayText.BitmapFont = _toolFontService.ToolFont;

            systemManagers.TextManager.Add(dimensionDisplayText, layer);
            this.systemManagers = systemManagers;

            endCap1 = new Line(systemManagers);
            endCap1.Name = "EndCap 1";
            AddLineToManagers(endCap1);

            endCap2 = new Line(systemManagers);
            endCap2.Name = "EndCap 2";
            AddLineToManagers(endCap2);
        }

        public void SetColor(Color lineColor, Color textColor)
        {
            endCap1.Color = lineColor;
            endCap2.Color = lineColor;

            middleLine.Color = lineColor;
            dimensionDisplayText.Color = textColor;
        }

        public void SetVisible(bool isVisible)
        {
            endCap1.Visible = endCap2.Visible = middleLine.Visible = dimensionDisplayText.Visible = isVisible;
        }

        public void Destroy()
        {
            systemManagers.ShapeManager.Remove(endCap1);
            systemManagers.ShapeManager.Remove(endCap2);

            systemManagers.ShapeManager.Remove(middleLine);

            systemManagers.TextManager.Remove(dimensionDisplayText);

        }

        #region Activity

        public void Activity(GraphicalUiElement objectToUpdateTo, WidthOrHeight widthOrHeight)
        {

            var asIpso = objectToUpdateTo as IRenderableIpso;

            var left = asIpso.GetAbsoluteX();
            var absoluteWidth = asIpso.Width;

            var top = asIpso.GetAbsoluteY();
            var absoluteHeight = asIpso.Height;

            var topLeft = new Vector2(left, top);

            // Adjust the Font size based on the UI's scale.  Instead of the Editors zoom level.
            // This should have ZERO affect for anyone not using UI level zoom
            float scaleFactor = _guiCommands.UiZoomValue / 100;
            float decreasedScaleFactor = scaleFactor * 0.75f;
            float finalizedScaleFactor = decreasedScaleFactor < 1 ? 1 : decreasedScaleFactor;

            // The dividing by zoom makes sure when we zoom the editor, it retains the same size for text 
            // even though the editor objects are changing in size.
            float adjustedScaleFactorWithEditorZoom = finalizedScaleFactor / Zoom; 

            // Apply zoom factors combined to the offsets and font scale
            float fromBodyOffset = 26  * adjustedScaleFactorWithEditorZoom;
            float endCapLength = 12 * adjustedScaleFactorWithEditorZoom;
            dimensionDisplayText.FontScale = adjustedScaleFactorWithEditorZoom;

            var rotationMatrix = asIpso.GetAbsoluteRotationMatrix();
            var rotatedLeftDirection = new Vector2(rotationMatrix.Left().X, rotationMatrix.Left().Y);
            var rotatedRightDirection = new Vector2(rotationMatrix.Right().X, rotationMatrix.Right().Y);
            var rotatedUpDirection = new Vector2(rotationMatrix.Down().X, rotationMatrix.Down().Y);
            var rotatedDownDirection = new Vector2(rotationMatrix.Up().X, rotationMatrix.Up().Y);
            var extraTextOffset = 4;

            if(widthOrHeight == WidthOrHeight.Width)
            {
                string suffix = null;
                switch (objectToUpdateTo.WidthUnits)
                {
                    case DataTypes.DimensionUnitType.MaintainFileAspectRatio:
                        suffix = " File Aspect Ratio";
                        break;
                    case DataTypes.DimensionUnitType.PercentageOfParent:
                        suffix = "% Parent";
                        break;
                    case DataTypes.DimensionUnitType.PercentageOfOtherDimension:
                        suffix = "% Height";
                        break;
                    case DataTypes.DimensionUnitType.PercentageOfSourceFile:
                        suffix = "% File";
                        break;
                    case DataTypes.DimensionUnitType.Ratio:
                        suffix = " Ratio of Parent";
                        break;
                    case DataTypes.DimensionUnitType.RelativeToChildren:
                        suffix = " Relative to Children";
                        break;
                    case DataTypes.DimensionUnitType.RelativeToParent:
                        suffix = " Relative to Parent";
                        break;
                }

                middleLine.SetPosition(topLeft + rotatedUpDirection * fromBodyOffset);
                middleLine.RelativePoint = rotatedRightDirection * absoluteWidth;

                if (suffix != null)
                {
                    dimensionDisplayText.RawText = $"{objectToUpdateTo.Width:0.0}{suffix}\n({absoluteWidth:0.0} px)";

                }
                else
                {
                    dimensionDisplayText.RawText = absoluteWidth.ToString("0.0");
                }

                var desiredPosition =
                    topLeft +
                    rotatedUpDirection * (fromBodyOffset + extraTextOffset + dimensionDisplayText.WrappedTextHeight) +
                    rotatedRightDirection * (-dimensionDisplayText.WrappedTextWidth / 2 + absoluteWidth / 2);

                desiredPosition = ClampPositionToCamera(desiredPosition);

                dimensionDisplayText.SetPosition(desiredPosition);
                //dimensionDisplayText.X = middleLine.X + absoluteWidth / 2.0f - dimensionDisplayText.WrappedTextWidth / 2.0f;
                //dimensionDisplayText.Y = top - fromBodyOffset - dimensionDisplayText.WrappedTextHeight;
                dimensionDisplayText.HorizontalAlignment = HorizontalAlignment.Center;
                dimensionDisplayText.VerticalAlignment = VerticalAlignment.Center;

                endCap1.SetPosition(middleLine.GetPosition() + rotatedUpDirection * endCapLength / 2.0f);
                endCap1.RelativePoint = rotatedDownDirection * endCapLength;

                endCap2.SetPosition(middleLine.GetPosition() + rotatedRightDirection * absoluteWidth + rotatedUpDirection * endCapLength / 2.0f);
                endCap2.RelativePoint = rotatedDownDirection * endCapLength;

            }
            else // height
            {
                string suffix = null;
                switch (objectToUpdateTo.HeightUnits)
                {
                    case DataTypes.DimensionUnitType.MaintainFileAspectRatio:
                        suffix = " File Aspect Ratio";
                        break;
                    case DataTypes.DimensionUnitType.PercentageOfParent:
                        suffix = "% Parent";
                        break;
                    case DataTypes.DimensionUnitType.PercentageOfOtherDimension:
                        suffix = "% Width";
                        break;
                    case DataTypes.DimensionUnitType.PercentageOfSourceFile:
                        suffix = "% File";
                        break;
                    case DataTypes.DimensionUnitType.Ratio:
                        suffix = " Ratio of Parent";
                        break;
                    case DataTypes.DimensionUnitType.RelativeToChildren:
                        suffix = " Relative to Children";
                        break;
                    case DataTypes.DimensionUnitType.RelativeToParent:
                        suffix = " Relative to Parent";
                        break;
                }

                // up is 0,1,0, which is actually down for Gum. Confusing, I know, but this results in the correct math
                middleLine.X = (topLeft + rotatedLeftDirection * fromBodyOffset).X;
                middleLine.Y = (topLeft + rotatedLeftDirection * fromBodyOffset).Y;

                middleLine.RelativePoint = rotatedDownDirection * absoluteHeight;

                if (suffix != null)
                {
                    dimensionDisplayText.RawText = $"{objectToUpdateTo.Height:0.0}{suffix}\n({absoluteHeight:0.0} px)";

                }
                else
                {
                    dimensionDisplayText.RawText = absoluteHeight.ToString("0.0");
                }

                var desiredPosition = topLeft + rotatedDownDirection * .5f * absoluteHeight + rotatedLeftDirection * (fromBodyOffset + extraTextOffset);

                desiredPosition.X = desiredPosition.X - dimensionDisplayText.WrappedTextWidth;
                desiredPosition.Y = desiredPosition.Y - dimensionDisplayText.WrappedTextHeight / 2.0f;

                desiredPosition = ClampPositionToCamera(desiredPosition);



                dimensionDisplayText.Position = desiredPosition;

                dimensionDisplayText.VerticalAlignment = VerticalAlignment.Center;

                endCap1.SetPosition(middleLine.GetPosition() + rotatedLeftDirection * endCapLength / 2.0f);
                endCap1.RelativePoint = rotatedRightDirection * endCapLength;

                endCap2.SetPosition(middleLine.GetPosition() + middleLine.RelativePoint + rotatedLeftDirection * endCapLength / 2.0f);
                endCap2.RelativePoint = rotatedRightDirection * endCapLength;
            }

            Vector2 ClampPositionToCamera(Vector2 desiredPosition)
            {
                var camera = systemManagers.Renderer.Camera;

                if (desiredPosition.X + dimensionDisplayText.WrappedTextWidth > camera.AbsoluteRight)
                {
                    desiredPosition.X = camera.AbsoluteRight - dimensionDisplayText.WrappedTextWidth;
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
                if (desiredPosition.Y + dimensionDisplayText.WrappedTextHeight > camera.AbsoluteBottom)
                {
                    desiredPosition.Y = camera.AbsoluteBottom - dimensionDisplayText.WrappedTextHeight;
                }

                return desiredPosition;
            }


            #endregion

        }
    }
}
