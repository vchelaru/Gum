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
        public ColoredRectangleRuntime(bool fullInstantiation = true)
        {
            if (fullInstantiation)
            {
                var solidRectangle = new SolidRectangle();
                SetContainedObject(solidRectangle);

                solidRectangle.Color = System.Drawing.Color.White;
                Width = 50;
                Height = 50;
            }
        }
    }
}
