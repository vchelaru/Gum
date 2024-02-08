using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving
{
    public class RectangleRuntime : GraphicalUiElement
    {
        RenderingLibrary.Math.Geometry.LineRectangle containedLineRectangle;
        RenderingLibrary.Math.Geometry.LineRectangle ContainedLineRectangle
        {
            get
            {
                if (containedLineRectangle == null)
                {
                    containedLineRectangle = this.RenderableComponent as RenderingLibrary.Math.Geometry.LineRectangle;
                }
                return containedLineRectangle;
            }
        }

        public bool IsDotted
        {
            get => ContainedLineRectangle.IsDotted;
            set
            {
                ContainedLineRectangle.IsDotted = value;
                NotifyPropertyChanged();
            }
        }

        public Microsoft.Xna.Framework.Color Color
        {
            get
            {
                return RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedLineRectangle.Color);
            }
            set
            {
                ContainedLineRectangle.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
                NotifyPropertyChanged();
            }
        }

        public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

        public RectangleRuntime(bool fullInstantiation = true)
        {
            if (fullInstantiation)
            {
                var rectangle = new RenderingLibrary.Math.Geometry.LineRectangle();
                SetContainedObject(rectangle);
                containedLineRectangle = rectangle;

                rectangle.Color = System.Drawing.Color.White;
                Width = 50;
                Height = 50;
            }
        }
    }
}
