using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Math.Geometry
{
    public class LinePolygon : IVisible, IRenderableIpso
    {
        #region Fields

        LinePrimitive mLinePrimitive;
        IRenderableIpso mParent;
        bool mVisible;
        List<IRenderableIpso> mChildren;

        #endregion

        #region Properties

        public string Name
        {
            get;
            set;
        }

        // this doesn't do anything.

        float IPositionedSizedObject.Width
        {
            get
            {
                float toReturn = 0;
                for(int i = 0; i < mLinePrimitive.VectorCount; i++)
                {
                    var absoluteX = System.Math.Abs(mLinePrimitive.PointAt(i).X);

                    if(absoluteX > toReturn)
                    {
                        toReturn = absoluteX;
                    }
                }

                return toReturn;
            }
            set
            {
                // do nothing
            }
        }
        float IPositionedSizedObject.Height
        {
            get
            {

                float toReturn = 0;
                for (int i = 0; i < mLinePrimitive.VectorCount; i++)
                {
                    var absoluteY = System.Math.Abs(mLinePrimitive.PointAt(i).Y);

                    if (absoluteY > toReturn)
                    {
                        toReturn = absoluteY;
                    }
                }

                return toReturn;
            }
            set
            {
                // do nothing
            }
        }
        bool IRenderable.Wrap { get { return false; } }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z
        {
            get;
            set;
        }


        public bool Visible
        {
            get { return mVisible; }
            set
            {
                mVisible = value;
            }
        }


        public Color Color
        {
            get
            {
                return mLinePrimitive.Color;
            }
            set
            {
                mLinePrimitive.Color = value;
            }
        }

        public BlendState BlendState
        {
            get { return BlendState.NonPremultiplied; }
        }


        bool IRenderableIpso.ClipsChildren
        {
            get
            {
                return false;
            }
        }

        public float Rotation
        {
            // even though it doesn't rotate itself, its children
            // can rotate, so it should store rotation values:
            get;
            set;
        }


        #endregion

        public LinePolygon() : this(null)
        {

        }

        public LinePolygon(SystemManagers managers)
        {

            mChildren = new List<IRenderableIpso>();

            Visible = true;

            if (managers != null)
            {
                mLinePrimitive = new LinePrimitive(managers.Renderer.SinglePixelTexture);
            }
            else
            {
                mLinePrimitive = new LinePrimitive(Renderer.Self.SinglePixelTexture);
            }

            // todo - make it default to something - a rectangle?
        }

        public void SetPoints(ICollection<Vector2> points)
        {
            mLinePrimitive.ClearVectors();

            if(points != null)
            {
                foreach(var point in points)
                {
                    mLinePrimitive.Add(point);
                }
            }
        }

        public bool HasCursorOver(float x, float y)
        {
            throw new NotImplementedException("// todo");
            // see if point is inside
        }

        void IRenderable.Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            if (AbsoluteVisible)
            {
                mLinePrimitive.Position.X = this.GetAbsoluteLeft();
                mLinePrimitive.Position.Y = this.GetAbsoluteTop();
                mLinePrimitive.Render(spriteRenderer, managers);
            }
        }

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

        public List<IRenderableIpso> Children
        {
            get { return mChildren; }
        }

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
        {
            mParent = parent;
        }

        public object Tag { get; set; }

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

        void IRenderable.PreRender() { }


    }
}
