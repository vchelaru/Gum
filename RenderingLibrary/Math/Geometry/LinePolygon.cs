using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BlendState = Gum.BlendState;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using System.Numerics;
using ToolsUtilitiesStandard.Helpers;

namespace RenderingLibrary.Math.Geometry
{
    public class LinePolygon : SpriteBatchRenderableBase, IVisible, IRenderableIpso
    {
        #region Fields

        LinePrimitive mLinePrimitive;
        IRenderableIpso mParent;
        bool mVisible;
        ObservableCollection<IRenderableIpso> mChildren;

        #endregion

        #region Properties

        public string Name
        {
            get;
            set;
        }
        ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;

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
        bool IRenderable.Wrap { get { return true; } }

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

        int IRenderableIpso.Alpha => Color.A;

        public BlendState BlendState
        {
            get { return BlendState.NonPremultiplied; }
        }

        public int PointCount => mLinePrimitive.VectorCount;

        bool IRenderableIpso.ClipsChildren
        {
            get
            {
                return false;
            }
        }

        float rotation;
        public float Rotation
        {
            get; set;
        }

        public bool FlipHorizontal { get; set; }

        public bool IsDotted { get; set; }

        public float LinePixelWidth
        {
            get => mLinePrimitive.LinePixelWidth;
            set => mLinePrimitive.LinePixelWidth = value;
        }

        bool IRenderableIpso.IsRenderTarget => false;


        #endregion

        #region Constructor

        public LinePolygon() : this(null)
        {

        }

        public LinePolygon(SystemManagers managers)
        {

            mChildren = new ObservableCollection<IRenderableIpso>();

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

        #endregion

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

        /// <summary>
        /// Returns whether the world X, Y values are inside of the polygon.
        /// </summary>
        /// <param name="worldX">The coordinate in world coordinates.</param>
        /// <param name="worldY"></param>
        /// <returns>Whether the argument x,y values are inside of the polygon.</returns>
        public bool IsPointInside(float worldX, float worldY)
        {
            // position has to be updated:
            mLinePrimitive.Position.X = this.GetAbsoluteLeft();
            mLinePrimitive.Position.Y = this.GetAbsoluteTop();
            return mLinePrimitive.IsPointInside(worldX, worldY, this.GetAbsoluteRotationMatrix());
            // see if point is inside
        }

        /// <summary>
        /// Returns the X,Y of the point at the argument index in object space (relative to the object's position)
        /// </summary>
        /// <param name="index">The 0-based index.</param>
        /// <returns>The position of the point at the argument index in object space.</returns>
        public Vector2 PointAt(int index)
        {
            return mLinePrimitive.PointAt(index);
        }

        public Vector2 AbsolutePointAt(int index)
        {
            var relativePoint = mLinePrimitive.PointAt(index);

            var rotationMatrix = this.GetAbsoluteRotationMatrix();

            return relativePoint.X * rotationMatrix.Right().ToVector2() + 
                relativePoint.Y * rotationMatrix.Up().ToVector2() +  
                new Vector2(this.GetAbsoluteX(), this.GetAbsoluteY());
        }

        public void InsertPointAt(Vector2 point, int index)
        {
            mLinePrimitive.Insert(index, point);
        }

        public void RemovePointAtIndex(int index)
        {
            mLinePrimitive.RemoveAt(index);
        }

        public void SetPointAt(Vector2 point, int index)
        {
            mLinePrimitive.SetPointAt(point, index);
        }

        public override void Render(ISystemManagers managers)
        {
            if (AbsoluteVisible)
            {
                mLinePrimitive.Position.X = this.GetAbsoluteLeft();
                mLinePrimitive.Position.Y = this.GetAbsoluteTop();

                Renderer renderer;
                var systemManagers = managers as SystemManagers;
                if (managers != null)
                {
                    renderer = managers.Renderer as Renderer;
                }
                else
                {
                    renderer = Renderer.Self;
                }

                var absoluteRotation = this.GetAbsoluteRotation();

                if (IsDotted)
                {
                    var textureToUse = renderer.DottedLineTexture;

                    mLinePrimitive.Render(renderer.SpriteRenderer, systemManagers, textureToUse, .1f * renderer.Camera.Zoom, rotation: absoluteRotation);
                }
                else
                {
                    var textureToUse = renderer.SinglePixelTexture;
                    if(renderer.SinglePixelSourceRectangle != null)
                    {
                        mLinePrimitive.Render(renderer.SpriteRenderer, systemManagers, textureToUse, 0, renderer.SinglePixelSourceRectangle, rotation: absoluteRotation);
                    }
                    else
                    {
                        mLinePrimitive.Render(renderer.SpriteRenderer, systemManagers, textureToUse, .1f * renderer.Camera.Zoom, rotation: absoluteRotation);
                    }
                }


                //mLinePrimitive.Render(spriteRenderer, managers, textureToUse, .2f * renderer.Camera.Zoom);
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

        void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
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
