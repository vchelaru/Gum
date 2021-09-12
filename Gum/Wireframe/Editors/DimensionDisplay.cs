using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe.Editors
{
    public enum WidthOrHeight
    {
        Width,
        Height
    }

    class DimensionDisplay
    {
        Line endCap1;
        Line endCap2;

        Line middleLine;
        Text dimensionDisplay;

        float Zoom => systemManagers.Renderer.Camera.Zoom;

        SystemManagers systemManagers;

        public void AddToManagers(SystemManagers systemManagers)
        {
            void AddLineToManagers(Line line) => systemManagers.ShapeManager.Add(line);

            middleLine = new Line(systemManagers);
            AddLineToManagers(middleLine);

            dimensionDisplay = new Text(systemManagers);
            dimensionDisplay.RenderBoundary = false;
            dimensionDisplay.Width = 0;
            dimensionDisplay.Height = 0;

            systemManagers.TextManager.Add(dimensionDisplay);
            this.systemManagers = systemManagers;

            endCap1 = new Line(systemManagers);
            AddLineToManagers(endCap1);

            endCap2 = new Line(systemManagers);
            AddLineToManagers(endCap2);
        }

        public void SetVisible(bool isVisible)
        {
            endCap1.Visible = endCap2.Visible = middleLine.Visible = dimensionDisplay.Visible = isVisible;
        }

        public void Destroy()
        {
            systemManagers.ShapeManager.Remove(endCap1);
            systemManagers.ShapeManager.Remove(endCap2);

            systemManagers.ShapeManager.Remove(middleLine);

            systemManagers.TextManager.Remove(dimensionDisplay);

        }

        public void Activity(GraphicalUiElement objectToUpdateTo, WidthOrHeight widthOrHeight)
        {
            var asIpso = objectToUpdateTo as IRenderableIpso;

            var left = asIpso.GetAbsoluteX();
            var absoluteWidth = asIpso.Width;

            var top = asIpso.GetAbsoluteY();
            var absoluteHeight = asIpso.Height;

            float fromBodyOffset = 26 / Zoom;
            float endCapLength = 12 / Zoom;
            dimensionDisplay.FontScale = 1/Zoom;

            if(widthOrHeight == WidthOrHeight.Width)
            {
                string suffix = null;
                switch(objectToUpdateTo.WidthUnits)
                {
                    case DataTypes.DimensionUnitType.MaintainFileAspectRatio:
                        suffix = " File Aspect Ratio";
                        break;
                    case DataTypes.DimensionUnitType.Percentage:
                        suffix = "% Container";
                        break;
                    case DataTypes.DimensionUnitType.PercentageOfOtherDimension:
                        suffix = "% Height";
                        break;
                    case DataTypes.DimensionUnitType.PercentageOfSourceFile:
                        suffix = "% File";
                        break;
                    case DataTypes.DimensionUnitType.Ratio:
                        suffix = " Ratio Container";
                        break;
                    case DataTypes.DimensionUnitType.RelativeToChildren:
                        suffix = " Relative to Children";
                        break;
                    case DataTypes.DimensionUnitType.RelativeToContainer:
                        suffix = " Relative to Container";
                        break;
                }

                middleLine.X = left;
                middleLine.RelativePoint.X = absoluteWidth;

                middleLine.Y = top - fromBodyOffset;
                middleLine.RelativePoint.Y = 0;

                if(suffix != null)
                {
                    dimensionDisplay.RawText = $"{objectToUpdateTo.Width:0.0}{suffix}\n({absoluteWidth:0.0} px)"; 

                }
                else
                {
                    dimensionDisplay.RawText = absoluteWidth.ToString("0.0");
                }

                dimensionDisplay.X = middleLine.X + absoluteWidth / 2.0f - dimensionDisplay.WrappedTextWidth / 2.0f;
                dimensionDisplay.Y = top - fromBodyOffset - dimensionDisplay.WrappedTextHeight;
                dimensionDisplay.HorizontalAlignment = HorizontalAlignment.Center;
                dimensionDisplay.VerticalAlignment = VerticalAlignment.Bottom;

                endCap1.X = left;
                endCap1.Y = middleLine.Y - endCapLength/2.0f;

                endCap1.RelativePoint.X = 0;
                endCap1.RelativePoint.Y = endCapLength;

                endCap2.X = left + absoluteWidth;
                endCap2.Y = endCap1.Y;

                endCap2.RelativePoint.X = 0;
                endCap2.RelativePoint.Y = endCapLength;
            }
            else // height
            {
                string suffix = null;
                switch (objectToUpdateTo.HeightUnits)
                {
                    case DataTypes.DimensionUnitType.MaintainFileAspectRatio:
                        suffix = " File Aspect Ratio";
                        break;
                    case DataTypes.DimensionUnitType.Percentage:
                        suffix = "% Container";
                        break;
                    case DataTypes.DimensionUnitType.PercentageOfOtherDimension:
                        suffix = "% Width";
                        break;
                    case DataTypes.DimensionUnitType.PercentageOfSourceFile:
                        suffix = "% File";
                        break;
                    case DataTypes.DimensionUnitType.Ratio:
                        suffix = " Ratio Container";
                        break;
                    case DataTypes.DimensionUnitType.RelativeToChildren:
                        suffix = " Relative to Children";
                        break;
                    case DataTypes.DimensionUnitType.RelativeToContainer:
                        suffix = " Relative to Container";
                        break;
                }


                middleLine.X = left - fromBodyOffset;
                middleLine.RelativePoint.X = 0;

                middleLine.Y = top;
                middleLine.RelativePoint.Y = absoluteHeight;

                if (suffix != null)
                {
                    dimensionDisplay.RawText = $"{objectToUpdateTo.Height:0.0}{suffix}\n({absoluteHeight:0.0} px)";

                }
                else
                {
                    dimensionDisplay.RawText = absoluteHeight.ToString("0.0");
                }
                dimensionDisplay.X = middleLine.X - 5 - dimensionDisplay.WrappedTextWidth;
                dimensionDisplay.Y = top + absoluteHeight / 2.0f - dimensionDisplay.WrappedTextHeight / 2.0f;
                dimensionDisplay.VerticalAlignment = VerticalAlignment.Center;

                endCap1.X = middleLine.X - endCapLength / 2.0f;
                endCap1.RelativePoint.X = endCapLength;

                endCap1.Y = top;
                endCap1.RelativePoint.Y = 0;

                endCap2.X = endCap1.X;
                endCap2.RelativePoint.X = endCap1.RelativePoint.X;

                endCap2.Y = top + absoluteHeight;
                endCap2.RelativePoint.Y = 0;
            }




        }
    }
}
