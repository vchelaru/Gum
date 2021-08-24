using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using System.Collections.ObjectModel;

namespace RenderingLibrary.Math.Geometry
{
    public class LineRectangle : IVisible, IRenderableIpso
    {
        #region Fields

        float mWidth = 32;
        float mHeight = 32;

        // Does position not update points? Is this a bug?
        public Vector2 Position;

        float mRotation;

        LinePrimitive mLinePrimitive;

        ObservableCollection<IRenderableIpso> mChildren;


        IRenderableIpso mParent;


        SystemManagers mManagers;

        bool mVisible;

        #endregion

        #region Properties

        ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;

        /// <summary>
        /// This is similar to the Visible property, but affects only this.
        /// This allows LineRectangles to not render without making their children invisible.
        /// </summary>
        public bool LocalVisible
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public float X
        {
            get => Position.X;
            set => Position.X = value;
        }

        public float Y
        {
            get => Position.Y;
            set => Position.Y = value;
        }

        public float Z
        {
            get;
            set;
        }


        public bool ClipsChildren
        {
            get;
            set;
        }

        public float Rotation
        {
            get
            {
                return mRotation;
            }
            set
            {
                mRotation = value;
                UpdatePoints();
            }
        }

        public bool FlipHorizontal { get; set; }

        public float Width
        {
            get { return mWidth; }
            set
            {
                mWidth = value;
                UpdatePoints();
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

        public float Height
        {
            get { return mHeight; }
            set
            {
                mHeight = value;
                UpdatePoints();
            }
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
            get => mLinePrimitive.Color;
            set => mLinePrimitive.Color = value;
        }

        public ObservableCollection<IRenderableIpso> Children
        {
            get { return mChildren; }
        }

        public object Tag { get; set; }

        public BlendState BlendState
        {
            get { return BlendState.NonPremultiplied; }
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

        public float LinePixelWidth
        {
            get { return mLinePrimitive.LinePixelWidth; }
            set { mLinePrimitive.LinePixelWidth = value; }
        }

        #endregion

        #region Methods

        public LineRectangle()
            : this(null)
        {

        }

        public LineRectangle(SystemManagers managers)
        {
            LocalVisible = true;

            mManagers = managers;

            mChildren = new ObservableCollection<IRenderableIpso>();

            Visible = true;
            Renderer renderer = null;
            if (mManagers != null)
            {
                renderer = mManagers.Renderer;
            }
            else
            {
                renderer = Renderer.Self;
            }
            mLinePrimitive = new LinePrimitive(renderer.SinglePixelTexture);
            mLinePrimitive.Add(0, 0);
            mLinePrimitive.Add(0, 0);
            mLinePrimitive.Add(0, 0);
            mLinePrimitive.Add(0, 0);
            mLinePrimitive.Add(0, 0);

            UpdatePoints();

            IsDotted = true;
        }

        private void UpdatePoints()
        {
            UpdateLinePrimitive(mLinePrimitive, this);

        }

        public static void UpdateLinePrimitive(LinePrimitive linePrimitive, IRenderableIpso ipso)
        {
            Matrix matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(ipso.GetAbsoluteRotation()));

            linePrimitive.Replace(0, Vector2.Zero);
            linePrimitive.Replace(1, Vector2.Transform(new Vector2(ipso.Width, 0), matrix) );
            linePrimitive.Replace(2, Vector2.Transform(new Vector2(ipso.Width, ipso.Height), matrix) );
            linePrimitive.Replace(3, Vector2.Transform(new Vector2(0, ipso.Height), matrix) );
            linePrimitive.Replace(4, Vector2.Zero); // close back on itself

        }


        void IRenderable.Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            if (AbsoluteVisible && LocalVisible)
            {
                // todo - add rotation
                RenderLinePrimitive(mLinePrimitive, spriteRenderer, this, managers, IsDotted);

            }
        }


        public static void RenderLinePrimitive(LinePrimitive linePrimitive, SpriteRenderer spriteRenderer, 
            IRenderableIpso ipso, SystemManagers managers, bool isDotted)
        {
            linePrimitive.Position.X = ipso.GetAbsoluteX();
            linePrimitive.Position.Y = ipso.GetAbsoluteY();

            Renderer renderer;
            if (managers != null)
            {
                renderer = managers.Renderer;
            }
            else
            {
                renderer = Renderer.Self;
            }

            Texture2D textureToUse = renderer.SinglePixelTexture;

            if (isDotted)
            {
                textureToUse = renderer.DottedLineTexture;
            }

            linePrimitive.Render(spriteRenderer, managers, textureToUse, .2f * renderer.Camera.Zoom);
        }

        #endregion

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
        {
            mParent = parent;
        }

        void IRenderable.PreRender() { }

        #region IVisible Members


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

        public override string ToString()
        {
            return Name + " (LineRectangle)";
        }
    }
}
