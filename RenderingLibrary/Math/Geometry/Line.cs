using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;

namespace RenderingLibrary.Math.Geometry
{
    public class Line : IRenderableIpso
    {
        #region Fields

        LinePrimitive mLinePrimitive;

        public Vector2 RelativePoint;


        IRenderableIpso mParent;

        ObservableCollection<IRenderableIpso> mChildren;
        SystemManagers mManagers;

        #endregion

        #region Properties


        public float Rotation { get; set; }

        public string Name
        {
            get;
            set;
        }

        public float X
        {
            get;
            set;
        }

        public float Y
        {
            get;
            set;
        }

        public float Z
        {
            get;
            set;
        }

        public bool Visible
        {
            get;
            set;
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


        bool IRenderableIpso.ClipsChildren
        {
            get
            {
                return false;
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

        public ObservableCollection<IRenderableIpso> Children
        {
            get { return mChildren; }
        }

        public object Tag
        {
            get;
            set;
        }

        private Renderer AssociatedRenderer
        {
            get
            {
                if (mManagers != null)
                {
                    return mManagers.Renderer;
                }
                else
                {
                    return Renderer.Self;
                }
            }
        }

        public bool IsDotted
        {
            get;
            set;
        }

        public bool Wrap
        {
            get { return true; }
        }

        #endregion


        public Line(SystemManagers managers)
        {
            mManagers = managers;

            Visible = true;
            if (mManagers != null)
            {
                mLinePrimitive = new LinePrimitive(mManagers.Renderer.SinglePixelTexture);
            }
            else
            {
                mLinePrimitive = new LinePrimitive(Renderer.Self.SinglePixelTexture);
            }

            mChildren = new ObservableCollection<IRenderableIpso>();
            UpdatePoints();
        }

        private void UpdatePoints()
        {
            while (mLinePrimitive.VectorCount < 2)
            {
                mLinePrimitive.Add(0, 0);
            }

            mLinePrimitive.Replace(1, this.RelativePoint);

            mLinePrimitive.Position.X = this.GetAbsoluteX();
            mLinePrimitive.Position.Y = this.GetAbsoluteY() ;
        }

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
        {
            mParent = parent;
        }

        void IRenderable.Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            UpdatePoints();
            if (Visible)
            {

                Texture2D textureToUse = AssociatedRenderer.SinglePixelTexture;

                if (IsDotted)
                {
                    textureToUse = AssociatedRenderer.DottedLineTexture;
                }

                mLinePrimitive.Render(spriteRenderer, managers, textureToUse, .2f * AssociatedRenderer.Camera.Zoom);
            }
        }

        void IRenderable.PreRender() { }

    }
}
