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
    public class TextRuntime : GraphicalUiElement
    {
        public TextRuntime(bool fullInstantiation = true)
        {
            if(fullInstantiation)
            {
                var textRenderable = new Text(SystemManagers.Default);
                textRenderable.RenderBoundary = false;
                
                SetContainedObject(textRenderable);
                Width = 0;
                WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
                Height = 0;
                HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

                textRenderable.RawText = "Hello World";
            }
        }
    }
}
