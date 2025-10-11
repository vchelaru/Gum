using System;
using RenderingLibrary.Graphics;
using System.Collections.ObjectModel;
using BlendState = Gum.BlendState;
using Color = System.Drawing.Color;

namespace RenderingLibrary.Math.Geometry
{
    public class LineGrid : SpriteBatchRenderableBase, IRenderableIpso
    {
        #region Fields

        LinePrimitive mLinePrimitive;


        float mRowWidth = 16;
        float mColumnWidth = 16;

        int mRowCount = 8;
        int mColumnCount = 8;

        public string Name { get; set; }

        #endregion

        #region Properties
        ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;

        public float RowWidth
        {
            get { return mRowWidth; }
            set
            {
                mRowWidth = value;
                UpdatePoints();
            }
        }

        public float ColumnWidth
        {
            get { return mColumnWidth; }
            set
            {
                mColumnWidth = value;
                UpdatePoints();
            }
        }

        public int RowCount
        {
            get { return mRowCount; }
            set
            {
                mRowCount = value;
                UpdatePoints();
            }
        }

        public int ColumnCount
        {
            get { return mColumnCount; }
            set
            {
                mColumnCount = value;
                UpdatePoints();
            }

        }

        public Color Color
        {
            get { return mLinePrimitive.Color; }
            set
            {
                mLinePrimitive.Color = value;
            }
        }

        int IRenderableIpso.Alpha => Color.A;
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

        public BlendState BlendState
        {
            get;
            set;
        }

        public bool Wrap
        {
            get { return true; }
        }

        bool IRenderableIpso.ClipsChildren
        {
            get
            {
                return false;
            }
        }

        IRenderableIpso IRenderableIpso.Parent
        {
            get
            {
                return null;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public ObservableCollection<IRenderableIpso> Children
        {
            get;
            private set;
        } = new ObservableCollection<IRenderableIpso>();

        BlendState IRenderable.BlendState
        {
            get
            {
                return BlendState.AlphaBlend;
            }
        }

        bool IRenderable.Wrap
        {
            get
            {
                return false;
            }
        }

        float IPositionedSizedObject.X
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        float IPositionedSizedObject.Y
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        float IPositionedSizedObject.Z
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        float IPositionedSizedObject.Rotation
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public bool FlipHorizontal { get; set; }


        float IPositionedSizedObject.Width
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        float IPositionedSizedObject.Height
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        string IPositionedSizedObject.Name
        {
            get;
            set;
        }

        object IPositionedSizedObject.Tag
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

        bool IRenderableIpso.IsRenderTarget => false;

        #endregion


        public LineGrid(SystemManagers managers)
        {
            Visible = true;
            if (managers != null)
            {
                mLinePrimitive = new LinePrimitive(managers.Renderer.SinglePixelTexture);
            }
            else
            {
                mLinePrimitive = new LinePrimitive(Renderer.Self.SinglePixelTexture);
            }

            mLinePrimitive.BreakIntoSegments = true;

            UpdatePoints();
        }


        void UpdatePoints()
        {
            mLinePrimitive.ClearVectors();

            float width = mColumnWidth * mColumnCount;
            float height = mRowWidth * mRowCount;

            float currentY = 0;

            for (int i = 0; i < mRowCount + 1; i++)
            {
                currentY = i * mRowWidth;
                mLinePrimitive.Add(0, currentY);
                mLinePrimitive.Add(width, currentY);
            }

            float currentX = 0;

            for (int i = 0; i < mColumnCount + 1; i++)
            {
                currentX = i * mColumnWidth;
                mLinePrimitive.Add(currentX, 0);
                mLinePrimitive.Add(currentX, height);
            }
        }


        void IRenderable.PreRender() { }


        public override void Render(ISystemManagers managers)
        {
            // See NineSlice for explanation of this Visible check
            //if (Visible)
            {
                var systemManagers = managers as SystemManagers;
                mLinePrimitive.Render(systemManagers.Renderer.SpriteRenderer, systemManagers);
            }
        }

        void IRenderableIpso.SetParentDirect(IRenderableIpso? newParent)
        {
            throw new NotImplementedException();
        }

        public override string ToString() => $"LineGrid {Name}";
    }
}
