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
    public class LineRectangle : IVisible, IPositionedSizedObject, IRenderable
    {
        #region Fields

        float mWidth = 32;
        float mHeight = 32;

        public Vector2 Position;

        LinePrimitive mLinePrimitive;

        List<IPositionedSizedObject> mChildren;


        IPositionedSizedObject mParent;


        SystemManagers mManagers;

        #endregion

        #region Properties

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
            get
            {
                return Position.X;
            }
            set
            {
                Position.X = value;
            }
        }

        public float Y
        {
            get
            {
                return Position.Y;
            }
            set
            {
                Position.Y = value;
            }
        }

        public float Z
        {
            get;
            set;
        }

        public float Width
        {
            get { return mWidth; }
            set
            {
                mWidth = value;
                UpdatePoints();
            }
        }

        public IPositionedSizedObject Parent
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

        public ICollection<IPositionedSizedObject> Children
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

            mChildren = new List<IPositionedSizedObject>();

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

        public static void UpdateLinePrimitive(LinePrimitive linePrimitive, IPositionedSizedObject ipso)
        {
            linePrimitive.Replace(0, Vector2.Zero);
            linePrimitive.Replace(1, new Vector2(ipso.Width, 0));
            linePrimitive.Replace(2, new Vector2(ipso.Width, ipso.Height));
            linePrimitive.Replace(3, new Vector2(0, ipso.Height));
            linePrimitive.Replace(4, Vector2.Zero); // close back on itself

        }

        void IRenderable.Render(SpriteBatch spriteBatch, SystemManagers managers)
        {
            if (Visible && LocalVisible)
            {
                RenderLinePrimitive(mLinePrimitive, spriteBatch, this, managers, IsDotted);
            }
        }

        public static void RenderLinePrimitive(LinePrimitive linePrimitive, SpriteBatch spriteBatch, IPositionedSizedObject ipso, SystemManagers managers, bool isDotted)
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

            linePrimitive.Render(spriteBatch, managers, textureToUse, .2f * renderer.Camera.Zoom);
        }

        #endregion

        void IPositionedSizedObject.SetParentDirect(IPositionedSizedObject parent)
        {
            mParent = parent;
        }

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
                return ((IPositionedSizedObject)this).Parent as IVisible;
            }
        }

        #endregion
    }
}
