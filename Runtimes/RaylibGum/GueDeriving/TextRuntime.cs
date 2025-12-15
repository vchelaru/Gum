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

    public float FontScale
    {
        get => ContainedText.FontScale;
        set
        {
            if (value != FontScale)
            {
                ContainedText.FontScale = value;
                NotifyPropertyChanged();
                UpdateLayout();
            }
        }
    }

    bool useCustomFont;
    /// <summary>
    /// Whether to use the CustomFontFile to determine the font value. 
    /// If false, then the font is determiend by looking for an existing
    /// font based on:
    /// * Font
    /// * FontSize
    /// * IsItalic
    /// * IsBold
    /// * UseFontSmoothing
    /// * OutlineThickness
    /// </summary>
    public bool UseCustomFont
    {
        get { return useCustomFont; }
        set { useCustomFont = value; UpdateToFontValues(); }
    }

    string customFontFile;
    /// <summary>
    /// Specifies the name of the custom font. This can be specified relative to
    /// FileManager.RelativeDirectory, which is the Content folder for code-only projects,
    /// or the folder containing the .gumx project if loading a Gum project. This should
    /// include the .fnt extension.
    /// </summary>
    public string CustomFontFile
    {
        get { return customFontFile; }
        set { customFontFile = value; UpdateToFontValues(); }
    }

    string font;
    /// <summary>
    /// The font name, such as "Arial", which is used to load fonts from 
    /// </summary>
    public string Font
    {
        get { return font; }
        set { font = value; UpdateToFontValues(); }
    }

    int fontSize;
    public int FontSize
    {
        get { return fontSize; }
        set { fontSize = value; UpdateToFontValues(); }
    }

    bool isItalic;
    public bool IsItalic
    {
        get => isItalic;
        set { isItalic = value; UpdateToFontValues(); }
    }

    bool isBold;
    public bool IsBold
    {
        get => isBold;
        set { isBold = value; UpdateToFontValues(); }
    }

    // Not sure if we need to make this a public value, but we do need to store it
    // Update - yes we do need this to be public so it can be assigned in codegen:
    bool useFontSmoothing = true;
    public bool UseFontSmoothing
    {
        get { return useFontSmoothing; }
        set { useFontSmoothing = value; UpdateToFontValues(); }
    }

    int outlineThickness;
    public int OutlineThickness
    {
        get { return outlineThickness; }
        set { outlineThickness = value; UpdateToFontValues(); }
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
