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
public class TextRuntime : InteractiveGue
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

    public Font CustomFont
    {
        get => ContainedText.Font;

        set => ContainedText.Font = value;
    }

    public string Text
    {
        get
        {
            return ContainedText.RawText;
        }
        set
        {
            var widthBefore = ContainedText.WrappedTextWidth;
            var heightBefore = ContainedText.WrappedTextHeight;
            if (this.WidthUnits == Gum.DataTypes.DimensionUnitType.RelativeToChildren)
            {
                // make it have no line wrap width before assignign the text:

                // todo - Vic needs to fix this up!
                //ContainedText.Width = null;
            }

            // Use SetProperty so it goes through the BBCode-checking methods
            //ContainedText.RawText = value;
            this.SetProperty("Text", value);

            NotifyPropertyChanged();
            var shouldUpdate = widthBefore != ContainedText.WrappedTextWidth || heightBefore != ContainedText.WrappedTextHeight;
            if (shouldUpdate)
            {
                UpdateLayout(
                    Gum.Wireframe.GraphicalUiElement.ParentUpdateType.IfParentWidthHeightDependOnChildren |
                    Gum.Wireframe.GraphicalUiElement.ParentUpdateType.IfParentStacks, int.MaxValue / 2);
            }
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
