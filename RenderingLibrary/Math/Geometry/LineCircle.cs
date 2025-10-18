using RenderingLibrary.Graphics;
using System.Collections.ObjectModel;
using BlendState = Gum.BlendState;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;
using static ToolsUtilitiesStandard.Helpers.Matrix4x4Extensions;

namespace RenderingLibrary.Math.Geometry
{
    public enum CircleOrigin
    {
        Center,
        TopLeft
    }

    public class LineCircle : SpriteBatchRenderableBase, IVisible, IRenderableIpso
    {
        #region Fields
        float mRadius;
        LinePrimitive mLinePrimitive;

        IRenderableIpso mParent;

        bool mVisible;

        ObservableCollection<IRenderableIpso> mChildren;

        CircleOrigin mCircleOrigin;

        #endregion

        #region Properties
        ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;

        public string Name
        {
            get;
            set;
        }

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

        public float Radius
        {
            get
            {
                return mRadius;
            }
            set
            {
                // Save a call to UpdatePoints
                if(value != mRadius)
                {
                    mRadius = value;
                    if(mVisible)
                    {
                        UpdatePoints();
                    }
                }
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

        public bool Wrap
        {
            get { return true; }
        }

        public CircleOrigin CircleOrigin
        {
            get
            {
                return mCircleOrigin;
            }
            set
            {
                mCircleOrigin = value;
                UpdatePoints();
            }
        }

        bool IRenderableIpso.IsRenderTarget => false;

        bool IRenderableIpso.ClipsChildren
        {
            get
            {
                return false;
            }
        }

        float mRotation;
        float lastAbsoluteRotation;
        public float Rotation
        {
            // even though it doesn't rotate itself, its children
            // can rotate, so it should store rotation values:
            get => mRotation;
            set
            {
                if(mRotation != value || lastAbsoluteRotation != this.GetAbsoluteRotation())
                {
                    lastAbsoluteRotation = this.GetAbsoluteRotation();
                    mRotation = value;
                    UpdatePoints();
                }
            }
        }

        public bool FlipHorizontal { get; set; }

        public float Width
        {
            get
            {
                return Radius * 2;
            }
            set
            {
                Radius = value / 2;
            }
        }

        public float Height
        {
            get
            {
                return Radius * 2;
            }
            set
            {
                Radius = value / 2;
            }
        }

        #endregion

        #region Methods


        public LineCircle() : this(null)
        {

        }

        public LineCircle(SystemManagers managers)
        {

            mChildren = new ObservableCollection<IRenderableIpso>();

            mRadius = 32;
            Visible = true;

            if (managers != null)
            {
                mLinePrimitive = new LinePrimitive(managers.Renderer.SinglePixelTexture);
            }
            else
            {
                mLinePrimitive = new LinePrimitive(Renderer.Self.SinglePixelTexture);
            }

            UpdatePoints();


        }

        const int SidesInCircle = 24;
        private void UpdatePoints()
        {

            mLinePrimitive.CreateCircle(Radius, SidesInCircle);

            if(mCircleOrigin == Geometry.CircleOrigin.TopLeft)
            {
                var rotation = this.GetAbsoluteRotation();
                if(rotation != 0)
                {
                    Matrix matrix = Matrix.CreateRotationZ(-MathHelper.ToRadians(rotation));

                    var vector = Radius * matrix.Right() + Radius * matrix.Up();

                    mLinePrimitive.Shift(vector.X, vector.Y);

                }
                else
                {
                    mLinePrimitive.Shift(Radius, Radius);
                }
            }
        }

        public bool HasCursorOver(float x, float y)
        {
            float radiusSquared = mRadius * mRadius;

            float distanceSquared = (x - mLinePrimitive.Position.X) * (distanceSquared = x - mLinePrimitive.Position.X) + 
                (y - mLinePrimitive.Position.Y) * (y - mLinePrimitive.Position.Y);
            return distanceSquared <= radiusSquared;
        }

        public override void Render(ISystemManagers managers)
        {
            // See NineSlice for explanation of this Visible check
            //if (AbsoluteVisible)
            {
                mLinePrimitive.Position.X = this.GetAbsoluteLeft();
                mLinePrimitive.Position.Y = this.GetAbsoluteTop();

                var systemManagers = managers as SystemManagers;
                var renderer = systemManagers.Renderer;
                mLinePrimitive.Render(
                    renderer.SpriteRenderer, 
                    systemManagers,
                    renderer.SinglePixelTexture,
                    // circles cannot be dotted, so pass 0 for the repetition
                    repetitionsPerLength:0,
                    renderer.SinglePixelSourceRectangle);
            }
        }
        #endregion


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
