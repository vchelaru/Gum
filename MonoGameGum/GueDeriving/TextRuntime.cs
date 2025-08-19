using Gum.DataTypes;
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
        set => ContainedText.HorizontalAlignment = value;
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
                ContainedText.Width = null;
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
