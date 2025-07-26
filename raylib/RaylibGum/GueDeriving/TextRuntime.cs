using Gum.Renderables;
using Gum.Renderables;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.GueDeriving;
public class TextRuntime : BindableGue
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

    public Color Color
    {
        get => ContainedText.Color;
        set
        {
            ContainedText.Color = value;
            NotifyPropertyChanged();
        }
    }

    public TextRuntime(bool fullInstantiation = true, SystemManagers? systemManagers = null)
    {
        if (fullInstantiation)
        {
            var textRenderable = new Text();
            mContainedText = textRenderable;

            SetContainedObject(textRenderable);

            //Width = DefaultWidth;
            //WidthUnits = DefaultWidthUnits;
            //Height = DefaultHeight;
            //HeightUnits = DefaultHeightUnits;
            //this.FontSize = DefaultFontSize;
            //this.Font = DefaultFont;
            //HasEvents = false;

            textRenderable.RawText = "Hello World";
        }
    }

}
