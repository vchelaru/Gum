using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SkiaGum.Renderables
{
    public enum GradientType
    {
        Linear,
        Radial
    }

    public class RenderableBase : IRenderableIpso, IVisible
    {
        Vector2 Position;
        IRenderableIpso mParent;

        public IRenderableIpso Parent
        {
            get { return mParent; }
            set
            {
                if (mParent != value)
                {
                    if (mParent != null)
                    {
                        mParent.Children.Remove(this);
                    }
                    mParent = value;
                    if (mParent != null)
                    {
                        mParent.Children.Add(this);
                    }
                }
            }
        }

        ObservableCollection<IRenderableIpso> mChildren;
        public ObservableCollection<IRenderableIpso> Children
        {
            get { return mChildren; }
        }

        public float X
        {
            get { return Position.X; }
            set { Position.X = value; }
        }

        public float Y
        {
            get { return Position.Y; }
            set { Position.Y = value; }
        }

        public float Z
        {
            get;
            set;
        }
        public float Width
        {
            get;
            set;
        }

        public float Height
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }
        public float Rotation { get; set; }

        public bool Wrap => false;


        public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;

        public float GradientX1 { get; set; }
        public GeneralUnitType GradientX1Units { get; set; }
        public float GradientY1 { get; set; }
        public GeneralUnitType GradientY1Units { get; set; }

        public float GradientX2 { get; set; }
        public float GradientY2 { get; set; }

        public float GradientInnerRadius { get; set; }
        public DimensionUnitType GradientInnerRadiusUnits { get; set; }

        public float GradientOuterRadius { get; set; }
        public DimensionUnitType GradientOuterRadiusUnits { get; set; }

        public bool FlipHorizontal
        {
            get;
            set;
        }

        public bool FlipVertical
        {
            get;
            set;
        }

        public object Tag { get; set; }

        public RenderableBase()
        {
            Width = 32;
            Height = 32;
            this.Visible = true;
            mChildren = new ObservableCollection<IRenderableIpso>();

        }

        public void Render(SKCanvas canvas)
        {
            if (AbsoluteVisible && Width > 0 && Height > 0)
            {
                var absoluteX = this.GetAbsoluteX();
                var absoluteY = this.GetAbsoluteY();
                var rect = new SKRect(absoluteX, absoluteY, absoluteX + this.Width, absoluteY + this.Height);

                DrawBound(rect, canvas);
            }
        }

        public virtual void DrawBound(SKRect boundingRect, SKCanvas canvas)
        {

        }

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
        {
            mParent = parent;
        }

        #region IVisible Implementation

        public bool Visible
        {
            get;
            set;
        }

        public bool AbsoluteVisible
        {
            get
            {
                if (((IVisible)this).Parent == null)
                {
                    return Visible;
                }
                else
                {
                    return Visible && ((IVisible)this).Parent.AbsoluteVisible;
                }
            }
        }

        IVisible IVisible.Parent
        {
            get
            {
                return ((IRenderableIpso)this).Parent as IVisible;
            }
        }

        #endregion

    }
}
