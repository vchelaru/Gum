using Gum.Converters;
using Gum.DataTypes;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region Alignment (Anchor/Dock)

        public void Dock(Dock dock)
        {
            switch (dock)
            {
                case Wireframe.Dock.Left:
                    this.XOrigin = HorizontalAlignment.Left;
                    this.XUnits = GeneralUnitType.PixelsFromSmall;
                    this.X = 0;

                    this.YOrigin = VerticalAlignment.Center;
                    this.YUnits = GeneralUnitType.PixelsFromMiddle;
                    this.Y = 0;

                    this.Height = 0;
                    this.HeightUnits = DimensionUnitType.RelativeToParent;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Left);
                        SetProperty("VerticalAlignment", VerticalAlignment.Center);
                    }
                    break;
                case Wireframe.Dock.Right:
                    this.XOrigin = HorizontalAlignment.Right;
                    this.XUnits = GeneralUnitType.PixelsFromLarge;
                    this.X = 0;

                    this.YOrigin = VerticalAlignment.Center;
                    this.YUnits = GeneralUnitType.PixelsFromMiddle;
                    this.Y = 0;

                    this.Height = 0;
                    this.HeightUnits = DimensionUnitType.RelativeToParent;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Right);
                        SetProperty("VerticalAlignment", VerticalAlignment.Center);
                    }
                    break;
                case Wireframe.Dock.Top:
                    this.XOrigin = HorizontalAlignment.Center;
                    this.XUnits = GeneralUnitType.PixelsFromMiddle;
                    this.X = 0;

                    this.YOrigin = VerticalAlignment.Top;
                    this.YUnits = GeneralUnitType.PixelsFromSmall;
                    this.Y = 0;

                    this.Width = 0;
                    this.WidthUnits = DimensionUnitType.RelativeToParent;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Center);
                        SetProperty("VerticalAlignment", VerticalAlignment.Top);
                    }
                    break;
                case Wireframe.Dock.Bottom:
                    this.XOrigin = HorizontalAlignment.Center;
                    this.XUnits = GeneralUnitType.PixelsFromMiddle;
                    this.X = 0;

                    this.YOrigin = VerticalAlignment.Bottom;
                    this.YUnits = GeneralUnitType.PixelsFromLarge;
                    this.Y = 0;

                    this.Width = 0;
                    this.WidthUnits = DimensionUnitType.RelativeToParent;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Center);
                        SetProperty("VerticalAlignment", VerticalAlignment.Bottom);
                    }
                    break;
                case Wireframe.Dock.Fill:
                    this.XOrigin = HorizontalAlignment.Center;
                    this.XUnits = GeneralUnitType.PixelsFromMiddle;
                    this.X = 0;

                    this.YOrigin = VerticalAlignment.Center;
                    this.YUnits = GeneralUnitType.PixelsFromMiddle;
                    this.Y = 0;

                    this.Width = 0;
                    this.WidthUnits = DimensionUnitType.RelativeToParent;

                    this.Height = 0;
                    this.HeightUnits = DimensionUnitType.RelativeToParent;

                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Center);
                        SetProperty("VerticalAlignment", VerticalAlignment.Center);
                    }
                    break;
                case Wireframe.Dock.FillHorizontally:
                    this.XOrigin = HorizontalAlignment.Center;
                    this.XUnits = GeneralUnitType.PixelsFromMiddle;
                    this.X = 0;

                    this.Width = 0;
                    this.WidthUnits = DimensionUnitType.RelativeToParent;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Center);
                    }
                    break;
                case Wireframe.Dock.FillVertically:
                    this.YOrigin = VerticalAlignment.Center;
                    this.YUnits = GeneralUnitType.PixelsFromMiddle;
                    this.Y = 0;
                    this.Height = 0;
                    this.HeightUnits = DimensionUnitType.RelativeToParent;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Center);
                        SetProperty("VerticalAlignment", VerticalAlignment.Center);
                    }
                    break;
                case Wireframe.Dock.SizeToChildren:
                    this.Width = 0;
                    this.WidthUnits = DimensionUnitType.RelativeToChildren;

                    this.Height = 0;
                    this.HeightUnits = DimensionUnitType.RelativeToChildren;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public Dock? GetDock()
        {

            if (this.XOrigin == HorizontalAlignment.Left &&
            this.XUnits == GeneralUnitType.PixelsFromSmall &&
            this.X == 0 &&

            this.YOrigin == VerticalAlignment.Center &&
            this.YUnits == GeneralUnitType.PixelsFromMiddle &&
            this.Y == 0 &&

            this.Height == 0 &&
            this.HeightUnits == DimensionUnitType.RelativeToParent)
                return Wireframe.Dock.Left;



            if (this.XOrigin == HorizontalAlignment.Right &&
            this.XUnits == GeneralUnitType.PixelsFromLarge &&
            this.X == 0 &&

            this.YOrigin == VerticalAlignment.Center &&
            this.YUnits == GeneralUnitType.PixelsFromMiddle &&
            this.Y == 0 &&

            this.Height == 0 &&
            this.HeightUnits == DimensionUnitType.RelativeToParent)
                return Wireframe.Dock.Right;


            if (this.XOrigin == HorizontalAlignment.Center &&
            this.XUnits == GeneralUnitType.PixelsFromMiddle &&
            this.X == 0 &&

            this.YOrigin == VerticalAlignment.Top &&
            this.YUnits == GeneralUnitType.PixelsFromSmall &&
            this.Y == 0 &&

            this.Width == 0 &&
            this.WidthUnits == DimensionUnitType.RelativeToParent)
                return Wireframe.Dock.Top;



            if (this.XOrigin == HorizontalAlignment.Center &&
            this.XUnits == GeneralUnitType.PixelsFromMiddle &&
            this.X == 0 &&

            this.YOrigin == VerticalAlignment.Bottom &&
            this.YUnits == GeneralUnitType.PixelsFromLarge &&
            this.Y == 0 &&

            this.Width == 0 &&
            this.WidthUnits == DimensionUnitType.RelativeToParent)
                return Wireframe.Dock.Bottom;




            if (this.XOrigin == HorizontalAlignment.Center &&
            this.XUnits == GeneralUnitType.PixelsFromMiddle &&
            this.X == 0 &&

            this.YOrigin == VerticalAlignment.Center &&
            this.YUnits == GeneralUnitType.PixelsFromMiddle &&
            this.Y == 0 &&

            this.Width == 0 &&
            this.WidthUnits == DimensionUnitType.RelativeToParent &&

            this.Height == 0 &&
            this.HeightUnits == DimensionUnitType.RelativeToParent)

                return Wireframe.Dock.Fill;




            if (this.XOrigin == HorizontalAlignment.Center &&
            this.XUnits == GeneralUnitType.PixelsFromMiddle &&
            this.X == 0 &&

            this.Width == 0 &&
            this.WidthUnits == DimensionUnitType.RelativeToParent)
                return Wireframe.Dock.FillHorizontally;




            if (this.YOrigin == VerticalAlignment.Center &&
            this.YUnits == GeneralUnitType.PixelsFromMiddle &&
            this.Y == 0 &&
            this.Height == 0 &&
            this.HeightUnits == DimensionUnitType.RelativeToParent)
                return Wireframe.Dock.FillVertically;




            if (this.Width == 0 &&
            this.WidthUnits == DimensionUnitType.RelativeToChildren &&

            this.Height == 0 &&
            this.HeightUnits == DimensionUnitType.RelativeToChildren)
                return Wireframe.Dock.SizeToChildren;

            return null;


        }

        public void Anchor(Anchor anchor)
        {
            switch (anchor)
            {
                case Wireframe.Anchor.TopLeft:
                    this.XOrigin = HorizontalAlignment.Left;
                    this.XUnits = GeneralUnitType.PixelsFromSmall;
                    this.X = 0;
                    this.YOrigin = VerticalAlignment.Top;
                    this.YUnits = GeneralUnitType.PixelsFromSmall;
                    this.Y = 0;

                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Left);
                        SetProperty("VerticalAlignment", VerticalAlignment.Top);
                    }
                    break;
                case Wireframe.Anchor.Top:
                    this.XOrigin = HorizontalAlignment.Center;
                    this.XUnits = GeneralUnitType.PixelsFromMiddle;
                    this.X = 0;
                    this.YOrigin = VerticalAlignment.Top;
                    this.YUnits = GeneralUnitType.PixelsFromSmall;
                    this.Y = 0;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Center);
                        SetProperty("VerticalAlignment", VerticalAlignment.Top);
                    }
                    break;
                case Wireframe.Anchor.TopRight:
                    this.XOrigin = HorizontalAlignment.Right;
                    this.XUnits = GeneralUnitType.PixelsFromLarge;
                    this.X = 0;
                    this.YOrigin = VerticalAlignment.Top;
                    this.YUnits = GeneralUnitType.PixelsFromSmall;
                    this.Y = 0;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Right);
                        SetProperty("VerticalAlignment", VerticalAlignment.Top);
                    }
                    break;
                case Wireframe.Anchor.Left:
                    this.XOrigin = HorizontalAlignment.Left;
                    this.XUnits = GeneralUnitType.PixelsFromSmall;
                    this.X = 0;
                    this.YOrigin = VerticalAlignment.Center;
                    this.YUnits = GeneralUnitType.PixelsFromMiddle;
                    this.Y = 0;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Left);
                        SetProperty("VerticalAlignment", VerticalAlignment.Center);
                    }
                    break;
                case Wireframe.Anchor.Center:
                    this.XOrigin = HorizontalAlignment.Center;
                    this.XUnits = GeneralUnitType.PixelsFromMiddle;
                    this.X = 0;
                    this.YOrigin = VerticalAlignment.Center;
                    this.YUnits = GeneralUnitType.PixelsFromMiddle;
                    this.Y = 0;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Center);
                        SetProperty("VerticalAlignment", VerticalAlignment.Center);
                    }
                    break;
                case Wireframe.Anchor.Right:
                    this.XOrigin = HorizontalAlignment.Right;
                    this.XUnits = GeneralUnitType.PixelsFromLarge;
                    this.X = 0;
                    this.YOrigin = VerticalAlignment.Center;
                    this.YUnits = GeneralUnitType.PixelsFromMiddle;
                    this.Y = 0;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Right);
                        SetProperty("VerticalAlignment", VerticalAlignment.Center);
                    }
                    break;
                case Wireframe.Anchor.BottomLeft:
                    this.XOrigin = HorizontalAlignment.Left;
                    this.XUnits = GeneralUnitType.PixelsFromSmall;
                    this.X = 0;
                    this.YOrigin = VerticalAlignment.Bottom;
                    this.YUnits = GeneralUnitType.PixelsFromLarge;
                    this.Y = 0;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Left);
                        SetProperty("VerticalAlignment", VerticalAlignment.Bottom);
                    }
                    break;
                case Wireframe.Anchor.Bottom:
                    this.XOrigin = HorizontalAlignment.Center;
                    this.XUnits = GeneralUnitType.PixelsFromMiddle;
                    this.X = 0;
                    this.YOrigin = VerticalAlignment.Bottom;
                    this.YUnits = GeneralUnitType.PixelsFromLarge;
                    this.Y = 0;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Center);
                        SetProperty("VerticalAlignment", VerticalAlignment.Bottom);
                    }
                    break;
                case Wireframe.Anchor.BottomRight:
                    this.XOrigin = HorizontalAlignment.Right;
                    this.XUnits = GeneralUnitType.PixelsFromLarge;
                    this.X = 0;
                    this.YOrigin = VerticalAlignment.Bottom;
                    this.YUnits = GeneralUnitType.PixelsFromLarge;
                    this.Y = 0;
                    if (RenderableComponent is IText)
                    {
                        SetProperty("HorizontalAlignment", HorizontalAlignment.Right);
                        SetProperty("VerticalAlignment", VerticalAlignment.Bottom);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public Anchor? GetAnchor()
        {
            if (this.XOrigin == HorizontalAlignment.Left &&
            this.XUnits == GeneralUnitType.PixelsFromSmall &&
            this.X == 0 &&
            this.YOrigin == VerticalAlignment.Top &&
            this.YUnits == GeneralUnitType.PixelsFromSmall &&
            this.Y == 0)
                return Wireframe.Anchor.TopLeft;


            if (this.XOrigin == HorizontalAlignment.Center &&
            this.XUnits == GeneralUnitType.PixelsFromMiddle &&
            this.X == 0 &&
            this.YOrigin == VerticalAlignment.Top &&
            this.YUnits == GeneralUnitType.PixelsFromSmall &&
            this.Y == 0)
                return Wireframe.Anchor.Top;

            if (this.XOrigin == HorizontalAlignment.Right &&
            this.XUnits == GeneralUnitType.PixelsFromLarge &&
            this.X == 0 &&
            this.YOrigin == VerticalAlignment.Top &&
            this.YUnits == GeneralUnitType.PixelsFromSmall &&
            this.Y == 0)
                return Wireframe.Anchor.TopRight;


            if (this.XOrigin == HorizontalAlignment.Left &&
            this.XUnits == GeneralUnitType.PixelsFromSmall &&
            this.X == 0 &&
            this.YOrigin == VerticalAlignment.Center &&
            this.YUnits == GeneralUnitType.PixelsFromMiddle &&
            this.Y == 0)
                return Wireframe.Anchor.Left;



            if (this.XOrigin == HorizontalAlignment.Center &&
            this.XUnits == GeneralUnitType.PixelsFromMiddle &&
            this.X == 0 &&
            this.YOrigin == VerticalAlignment.Center &&
            this.YUnits == GeneralUnitType.PixelsFromMiddle &&
            this.Y == 0)
                return Wireframe.Anchor.Center;



            if (this.XOrigin == HorizontalAlignment.Right &&
            this.XUnits == GeneralUnitType.PixelsFromLarge &&
            this.X == 0 &&
            this.YOrigin == VerticalAlignment.Center &&
            this.YUnits == GeneralUnitType.PixelsFromMiddle &&
            this.Y == 0)
                return Wireframe.Anchor.Right;

            if (this.XOrigin == HorizontalAlignment.Left &&
            this.XUnits == GeneralUnitType.PixelsFromSmall &&
            this.X == 0 &&
            this.YOrigin == VerticalAlignment.Bottom &&
            this.YUnits == GeneralUnitType.PixelsFromLarge &&
            this.Y == 0)
                return Wireframe.Anchor.BottomLeft;

            if (this.XOrigin == HorizontalAlignment.Center &&
            this.XUnits == GeneralUnitType.PixelsFromMiddle &&
            this.X == 0 &&
            this.YOrigin == VerticalAlignment.Bottom &&
            this.YUnits == GeneralUnitType.PixelsFromLarge &&
            this.Y == 0)
                return Wireframe.Anchor.Bottom;

            if (this.XOrigin == HorizontalAlignment.Right &&
            this.XUnits == GeneralUnitType.PixelsFromLarge &&
            this.X == 0 &&
            this.YOrigin == VerticalAlignment.Bottom &&
            this.YUnits == GeneralUnitType.PixelsFromLarge &&
            this.Y == 0)
                return Wireframe.Anchor.BottomRight;

            return null;
        }

        #endregion
    }
}