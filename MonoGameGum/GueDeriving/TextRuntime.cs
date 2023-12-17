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
        Text mContainedText;
        Text ContainedText
        {
            get
            {
                if (mContainedText == null)
                {
                    mContainedText = this.RenderableComponent as Text;
                }
                return mContainedText;
            }
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get => ContainedText.HorizontalAlignment;
            set => ContainedText.HorizontalAlignment = value;
        }

        public VerticalAlignment VerticalAlignment
        {
            get => ContainedText.VerticalAlignment;
            set => ContainedText.VerticalAlignment = value;
        }

        public string Text
        {
            get => ContainedText.RawText;
            set => ContainedText.RawText = value;
        }


        public TextRuntime(bool fullInstantiation = true)
        {
            if(fullInstantiation)
            {
                var textRenderable = new Text(SystemManagers.Default);
                textRenderable.RenderBoundary = false;
                
                SetContainedObject(textRenderable);
                Width = 0;
                WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                Height = 0;
                HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

                textRenderable.RawText = "Hello World";
            }
        }
    }
}
