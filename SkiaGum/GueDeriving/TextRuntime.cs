#if MONOGAME || KNI || XNA4 || FNA
#define XNALIKE
#endif
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;
using System;

namespace SkiaGum.GueDeriving;

/// <summary>
/// A visual text element which can display a string.
/// </summary>
public class TextRuntime : BindableGue
{
    #region Skia-specific properties, which may go away in the future
    public static int DefaultRed { get; set; } = 69;
    public static int DefaultGreen { get; set; } = 90;
    public static int DefaultBlue { get; set; } = 100;


    public enum ColorCategory
    {
        White,
        DefaultColor,
        LightBlue,
        LightGray
    }

    [Obsolete("This existed to match Gum, but this should be handled by codegen")]
    ColorCategory mColorCategoryState;
    [Obsolete("This existed to match Gum, but this should be handled by codegen")]
    public ColorCategory ColorCategoryState
    {
        get => mColorCategoryState;
        set
        {
            mColorCategoryState = value;
            switch (value)
            {
                case ColorCategory.White:
                    this.Blue = 255;
                    this.Green = 255;
                    this.Red = 255;
                    break;
                case ColorCategory.DefaultColor:
                    this.Blue = 100;
                    this.Green = 90;
                    this.Red = 69;
                    break;
                case ColorCategory.LightBlue:
                    this.Blue = 193;
                    this.Green = 145;
                    this.Red = 0;
                    break;
                case ColorCategory.LightGray:
                    this.Blue = 227;
                    this.Green = 226;
                    this.Red = 226;
                    break;
            }
        }
    }

    [Obsolete("Use MaxNumberOfLines instead")]
    public int? MaximumNumberOfLines
    {
        get => MaxNumberOfLines;
        set => MaxNumberOfLines = value;
    }

    #endregion

    Text? mContainedText;
    Text ContainedText
    {
        get
        {
            if(mContainedText == null)
            {
                mContainedText = (Text)this.RenderableComponent;
            }
            return mContainedText;
        }
    }


#if !RAYLIB && !SKIA
    /// <summary>
    /// The XNA blend state used when rendering the text. This controls how 
    /// color and alpha values blend with the background.
    /// </summary>
    public Microsoft.Xna.Framework.Graphics.BlendState BlendState
    {
        get => ContainedText.BlendState.ToXNA();
        set
        {
            ContainedText.BlendState = value.ToGum();
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(Blend));
        }
    }

    public Gum.RenderingLibrary.Blend Blend
    {
        get
        {
            return Gum.RenderingLibrary.BlendExtensions.ToBlend(ContainedText.BlendState);
        }
        set
        {
            BlendState = value.ToBlendState().ToXNA();
            // NotifyPropertyChanged handled by BlendState:
        }
    }
#endif

    /// <summary>
    /// The red component of the text color. Ranges from 0 to 255.
    /// </summary>
    public int Red
    {
        get => ContainedText.Red;
        set => ContainedText.Red = value;
    }

    /// <summary>
    /// The green component of the text color. Ranges from 0 to 255.
    /// </summary>
    public int Green
    {
        get => ContainedText.Green;
        set => ContainedText.Green = value;
    }

    /// <summary>
    /// The blue component of the text color. Ranges from 0 to 255.
    /// </summary>
    public int Blue
    {
        get => ContainedText.Blue;
        set => ContainedText.Blue = value;
    }

    /// <summary>
    /// The alpha (opacity) component of the text color. Ranges from 0 (fully transparent) to 255 (fully opaque).
    /// </summary>
    public int Alpha
    {
        get => ContainedText.Alpha;
        set => ContainedText.Alpha = value;
    }

    /// <summary>
    /// Gets or sets the color used to render the text. This includes color and alpha (opacity) components.
    /// </summary>
    public SKColor Color
    {
        get => ContainedText.Color;
        set => ContainedText.Color = value;
    }

    /// <summary>
    /// The horizontal alignment of the text within its bounding box.
    /// </summary>
    public HorizontalAlignment HorizontalAlignment
    {
        get => ContainedText.HorizontalAlignment;
        set => ContainedText.HorizontalAlignment = value;
    }

    /// <summary>
    /// The vertical alignment of the text within its bounding box.
    /// </summary>
    public VerticalAlignment VerticalAlignment
    {
        get => ContainedText.VerticalAlignment;
        set => ContainedText.VerticalAlignment = value;
    }

#if !RAYLIB && !SKIA
    /// <summary>
    /// The maximum letters to display. This can be used to 
    /// create an effect where the text prints out letter-by-letter.
    /// </summary>
    public int? MaxLettersToShow
    {
        get => mContainedText.MaxLettersToShow;
        set
        {
            mContainedText.MaxLettersToShow = value;
        }
    }
#endif

#if !RAYLIB
    /// <summary>
    /// The maximum number of lines to display. This can be used to 
    /// limit how many lines of text are displayed at one time.
    /// </summary>
    public int? MaxNumberOfLines
    {
        get => mContainedText.MaxNumberOfLines;
        set => mContainedText.MaxNumberOfLines = value;
    }
#endif


#if !SKIA
    public BitmapFont BitmapFont
    {
        get => ContainedText.BitmapFont;
        set
        {
            if (value != BitmapFont)
            {
                ContainedText.BitmapFont = value;
                NotifyPropertyChanged();
                UpdateLayout();
            }
        }
    }
#endif

        /// <summary>
        /// A multiplier used when rendering the text. The default value is 1.0.
        /// </summary>
#if RAYLIB || XNALIKE
    /// <remarks>
    /// Setting this value to a value other than 1 scales the text accordingly. This is
    /// a scalue value applied to the existing font, so a value larger than 1 can result
    /// in the font appearing pixellated.
    /// 
    /// Since this value does not affect the underlying Font, it can be changed without
    /// requiring a dedicated font asset.
    /// </remarks>
#endif
    public float FontScale
    {
        get => ContainedText.FontScale;
        set => ContainedText.FontScale = value;
    }

    public float LineHeightMultiplier
    {
        get => ContainedText.LineHeightMultiplier;
        set => ContainedText.LineHeightMultiplier = value;
    }

    TextOverflowHorizontalMode textOverflowHorizontalMode;
    public TextOverflowHorizontalMode TextOverflowHorizontalMode
    {
        get => textOverflowHorizontalMode;
        set
        {
            textOverflowHorizontalMode = value;
            if (textOverflowHorizontalMode == TextOverflowHorizontalMode.EllipsisLetter)
            {
                ContainedText.IsTruncatingWithEllipsisOnLastLine = true;
            }
            else
            {
                ContainedText.IsTruncatingWithEllipsisOnLastLine = false;
            }
        }
    }


    public string Text
    {
        get => ContainedText.RawText;
        set => ContainedText.RawText = value;
    }

    public bool IsBold
    {
        get => mContainedText.BoldWeight > 1;
        set
        {
            if(value)
            {
                mContainedText.BoldWeight = 1.5f;
            }
            else
            {
                mContainedText.BoldWeight = 1;
            }
        }
    }

    /// <summary>
    /// Gets or sets the weight multiplier for bold text rendering.
    /// A value of 1.0 represents regular weight, while higher values 
    /// increase the thickness of strokes (e.g., 1.5 for bold).
    /// </summary>
    public float BoldWeight
    {
        get => mContainedText.BoldWeight;
        set => mContainedText.BoldWeight = value;
    }

    //public SKTypeface FontType
    //{
    //    get => ContainedText.Font;
    //    set => ContainedText.Font = value;
    //}

    public int FontSize
    {
        get => ContainedText.FontSize;
        // July 10, 2023 - This is causing problems
        // because the FontSize is not making it to the
        // underyling object. I'm going to do both for now
        // as a half-step to removing the usage of the ContainedText...
        // or maybe we always use both?
        //set => ContainedText.FontSize = value;
        set
        {
            ContainedText.FontSize = value;
            UpdateToFontValues();
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

    bool isItalic;
    public bool IsItalic
    {
        get => isItalic;
        set { isItalic = value; UpdateToFontValues(); }
    }

    //// Not sure if we need to make this a public value, but we do need to store it
    //// Update - yes we do need this to be public so it can be assigned in codegen:
    //bool useFontSmoothing = true;
    //public bool UseFontSmoothing
    //{
    //    get { return useFontSmoothing; }
    //    set { useFontSmoothing = value; UpdateToFontValues(); }
    //}

    int outlineThickness;
    public int OutlineThickness
    {
        get { return outlineThickness; }
        set { outlineThickness = value; UpdateToFontValues(); }
    }

    public TextRuntime (bool fullInstantiation = true)
    {
        if(fullInstantiation)
        {
            SetContainedObject(new Text());

            this.Height = 0;
            this.HeightUnits = DimensionUnitType.RelativeToChildren;
            this.Width = 0;
            this.WidthUnits = DimensionUnitType.RelativeToChildren;

            // These values are default values matching Gum defaults. Not sure how to handle this - ultimately the Gum project
            // could change these values, in which case these would no longer be valid. We need a way to push the default states
            // from Gum here. But...for now at least we'll match defaults:
            FontSize = 18;

            Red = 255;
            Green = 255;
            Blue = 255;

            this.Text = "Hello";
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (TextRuntime)base.Clone();

        toReturn.mContainedText = null;

        return toReturn;
    }
}
