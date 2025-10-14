﻿using Gum.DataTypes;
using Gum.RenderingLibrary;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving;

public class TextRuntime : InteractiveGue
{
    Text? mContainedText;
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

    // Shouldn't this be an XNA blend state?
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

    public int Red
    {
        get => mContainedText.Red;
        set => mContainedText.Red = value;
    }

    public int Green
    {
        get => mContainedText.Green;
        set => mContainedText.Green = value;
    }

    public int Blue
    {
        get => mContainedText.Blue;
        set => mContainedText.Blue = value;
    }

    public int Alpha
    {
        get => mContainedText.Alpha;
        set => mContainedText.Alpha = value;
    }


    public Microsoft.Xna.Framework.Color Color
    {
        get
        {
            return RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedText.Color);
        }
        set
        {
            ContainedText.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
            NotifyPropertyChanged();
        }
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get => ContainedText.HorizontalAlignment;
        set
        {
            ContainedText.HorizontalAlignment = value;
            NotifyPropertyChanged();
        }
    }

    public VerticalAlignment VerticalAlignment
    {
        get => ContainedText.VerticalAlignment;
        set => ContainedText.VerticalAlignment = value;
    }

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

    public float LineHeightMultiplier
    {
        get => ContainedText.LineHeightMultiplier;
        set
        {
            if (value != LineHeightMultiplier)
            {
                ContainedText.LineHeightMultiplier = value;
                NotifyPropertyChanged();
                UpdateLayout();
            }
        }
    }

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

    /// <summary>
    /// The maximum number of lines to display. This can be used to 
    /// limit how many lines of text are displayed at one time.
    /// </summary>
    public int? MaxNumberOfLines
    {
        get => mContainedText.MaxNumberOfLines;
        set
        {
            mContainedText.MaxNumberOfLines = value;
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

    string? customFontFile;
    /// <summary>
    /// Specifies the name of the custom font. This can be specified relative to
    /// FileManager.RelativeDirectory, which is the Content folder for code-only projects,
    /// or the folder containing the .gumx project if loading a Gum project. This should
    /// include the .fnt extension.
    /// </summary>
    public string? CustomFontFile
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

    public TextOverflowHorizontalMode TextOverflowHorizontalMode
    {
        // Currently GraphicalUiElement doesn't expose this property so we have to go through setting it by string:
        get => ContainedText.IsTruncatingWithEllipsisOnLastLine ? TextOverflowHorizontalMode.EllipsisLetter : TextOverflowHorizontalMode.TruncateWord;
        set
        {
            ContainedText.IsTruncatingWithEllipsisOnLastLine = value == TextOverflowHorizontalMode.EllipsisLetter;
            NotifyPropertyChanged();
            UpdateLayout();
        }
    }

    /// <summary>
    /// Gets or sets the text rendering position mode to use for the contained text, overriding the default behavior if
    /// specified.
    /// </summary>
    /// <remarks>Set this property to control how text positioning is handled during rendering. If the value
    /// is <see langword="null"/>, the default rendering position mode is used. Changing this property affects only the
    /// contained text and does not impact other elements.</remarks>
    public TextRenderingPositionMode? TextRenderingPositionMode
    {
        get => ContainedText.OverrideTextRenderingPositionMode;
        set => ContainedText.OverrideTextRenderingPositionMode = value;
    }

    public string? Text
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
                if(this.MaxWidth == null)
                {
                    // make it have no line wrap width before assignign the text:
                    ContainedText.Width = null;
                }
                else
                {
                    ContainedText.Width = this.MaxWidth;
                }
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

    #region Defaults



    // todo - add more here
    public static string DefaultFont = "Arial";
    public static int DefaultFontSize = 18;

    public float DefaultWidth = 0;
    public float DefaultHeight = 0;

    public DimensionUnitType DefaultWidthUnits = DimensionUnitType.RelativeToChildren;
    public DimensionUnitType DefaultHeightUnits = DimensionUnitType.RelativeToChildren;

    #endregion

    public TextRuntime(bool fullInstantiation = true, SystemManagers? systemManagers = null)
    {
        if(fullInstantiation)
        {
            var textRenderable = new Text(systemManagers ?? SystemManagers.Default);
            textRenderable.RenderBoundary = false;
            mContainedText = textRenderable;
            
            SetContainedObject(textRenderable);

            Width = DefaultWidth;
            WidthUnits = DefaultWidthUnits;
            Height = DefaultHeight;
            HeightUnits = DefaultHeightUnits;
            this.FontSize = DefaultFontSize;
            this.Font = DefaultFont;
            HasEvents = false;

            textRenderable.RawText = "Hello World";
        }
    }

    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer:null);
}
