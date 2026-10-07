using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.RenderingLibrary;
using GumDataTypes.Variables;

using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ComponentModel;
using ToolsUtilitiesStandard.Helpers;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using GumRuntime;
using Gum.Collections;

#if FRB

using InteractiveGue = Gum.Wireframe.GraphicalUiElement;
#endif


#if !FRB
using Gum.StateAnimation.Runtime;
#endif

namespace Gum.Wireframe;

public partial class GraphicalUiElement
{

    #region Alignment (Anchor/Dock)

    public void Dock(Dock dock)
    {
        var wasSuspended = GraphicalUiElement.IsAllLayoutSuspended || this.IsLayoutSuspended;

        if (!wasSuspended)
        {
            this.SuspendLayout();
        }

        ApplyDock(dock);

        if (!wasSuspended)
        {
            this.ResumeLayout();
        }
    }

    private void ApplyDock(Dock dock)
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

    /// <summary>
    /// Sets the X, Y, Units and Origin values for an element, based on the Anchor Enum types.  If the element implements IText, also sets Text Alignment.
    /// </summary>
    /// <param name="anchor"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Anchor(Anchor anchor)
    {
        var wasSuspended = GraphicalUiElement.IsAllLayoutSuspended || this.IsLayoutSuspended;

        if(!wasSuspended)
        {
            this.SuspendLayout();
        }

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
            case Wireframe.Anchor.CenterHorizontally:
                this.XOrigin = HorizontalAlignment.Center;
                this.XUnits = GeneralUnitType.PixelsFromMiddle;
                this.X = 0;
                if (RenderableComponent is IText)
                {
                    SetProperty("HorizontalAlignment", HorizontalAlignment.Center);
                }
                break;
            case Wireframe.Anchor.CenterVertically:
                this.YOrigin = VerticalAlignment.Center;
                this.YUnits = GeneralUnitType.PixelsFromMiddle;
                this.Y = 0;
                if (RenderableComponent is IText)
                {
                    SetProperty("VerticalAlignment", VerticalAlignment.Center);
                }
                break;
            default:
                throw new NotImplementedException();
        }

        if(!wasSuspended)
        {
            this.ResumeLayout();
        }
    }

    /// <summary>
    /// Attempts to determine if the element is anchored utilizing the X, Y, Units, and Origins.  
    /// </summary>
    /// <returns>If a suitable Anchor position is identified, returns that Anchor Enum, otherwise null.</returns>
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

        if (this.XOrigin == HorizontalAlignment.Center &&
        this.XUnits == GeneralUnitType.PixelsFromMiddle &&
        this.X == 0)
            return Wireframe.Anchor.CenterHorizontally;

        if (this.YOrigin == VerticalAlignment.Center &&
        this.YUnits == GeneralUnitType.PixelsFromMiddle &&
        this.Y == 0)
            return Wireframe.Anchor.CenterVertically;

        return null;
    }

    #endregion
}
