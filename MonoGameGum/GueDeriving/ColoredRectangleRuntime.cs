using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving
{
    public class ColoredRectangleRuntime : GraphicalUiElement
    {
        RenderingLibrary.Graphics.SolidRectangle mContainedColoredRectangle;
        RenderingLibrary.Graphics.SolidRectangle ContainedColoredRectangle
        {
            get
            {
                if (mContainedColoredRectangle == null)
                {
                    mContainedColoredRectangle = this.RenderableComponent as RenderingLibrary.Graphics.SolidRectangle;
                }
                return mContainedColoredRectangle;
            }
        }

        public int Alpha
        {
            get
            {
                return ContainedColoredRectangle.Alpha;
            }
            set
            {
                ContainedColoredRectangle.Alpha = value;
                NotifyPropertyChanged();
            }
        }
        public Gum.RenderingLibrary.Blend Blend
        {
            get
            {
                return Gum.RenderingLibrary.BlendExtensions.ToBlend(ContainedColoredRectangle.BlendState);
            }
            set
            {
                ContainedColoredRectangle.BlendState = Gum.RenderingLibrary.BlendExtensions.ToBlendState(value);
                NotifyPropertyChanged();
            }
        }
        public int Blue
        {
            get
            {
                return ContainedColoredRectangle.Blue;
            }
            set
            {
                ContainedColoredRectangle.Blue = value;
                NotifyPropertyChanged();
            }
        }
        public int Green
        {
            get
            {
                return ContainedColoredRectangle.Green;
            }
            set
            {
                ContainedColoredRectangle.Green = value;
                NotifyPropertyChanged();
            }
        }
        public int Red
        {
            get
            {
                return ContainedColoredRectangle.Red;
            }
            set
            {
                ContainedColoredRectangle.Red = value;
                NotifyPropertyChanged();
            }
        }
        public Microsoft.Xna.Framework.Color Color
        {
            get
            {
                return RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedColoredRectangle.Color);
            }
            set
            {
                ContainedColoredRectangle.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
                NotifyPropertyChanged();
            }
        }


        public ColoredRectangleRuntime(bool fullInstantiation = true)
        {
            if (fullInstantiation)
            {
                var solidRectangle = new SolidRectangle();
                SetContainedObject(solidRectangle);
                mContainedColoredRectangle = solidRectangle;

                solidRectangle.Color = System.Drawing.Color.White;
                Width = 50;
                Height = 50;
            }
        }
    }
}
