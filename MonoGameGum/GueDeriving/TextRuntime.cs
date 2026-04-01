#if MONOGAME || KNI || XNA4 || FNA
#define XNALIKE
#endif
using Gum.DataTypes;
#if RAYLIB
using Gum.Renderables;
#else
using Gum.Graphics;
using Gum.RenderingLibrary;
#endif
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB
using Raylib_cs;
namespace Gum.GueDeriving;
#else
using Color = Microsoft.Xna.Framework.Color;
namespace MonoGameGum.GueDeriving;
#endif

/// <summary>
/// A visual text element which can display a string.
/// </summary>
public class TextRuntime : InteractiveGue
{
    Text? mContainedText;
    Text ContainedText
    {
        get
        {
            if (mContainedText == null)
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

    public Gum.RenderingLibrary.Blend? Blend
    {
        get
        {
            return Gum.RenderingLibrary.BlendExtensions.ToBlend(ContainedText.BlendState);
        }
        set
        {
            if (value.HasValue)
            {
                BlendState = value.Value.ToBlendState().ToXNA();
            }
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
    public Color Color
    {
#if XNALIKE
        get => RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedText.Color);
        set
        {
            ContainedText.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
            NotifyPropertyChanged();
        }
#else
        get => ContainedText.Color;
        set
        {
            ContainedText.Color = value;
            NotifyPropertyChanged();
        }
#endif
    }

    /// <summary>
    /// The horizontal alignment of the text within its bounding box.
    /// </summary>
    public HorizontalAlignment HorizontalAlignment
    {
        get => ContainedText.HorizontalAlignment;
        set
        {
            ContainedText.HorizontalAlignment = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The vertical alignment of the text within its bounding box.
    /// </summary>
    public VerticalAlignment VerticalAlignment
    {
        get => ContainedText.VerticalAlignment;
        set => ContainedText.VerticalAlignment = value;
    }

#if !SKIA
    /// <summary>
    /// The maximum number of characters to display visually. Characters beyond this count
    /// are hidden but remain in the text string. This is a display-only
    /// property useful for typewriter-style effects where text prints out letter-by-letter.
    /// </summary>
    public int? MaxLettersToShow
    {
        get => ContainedText.MaxLettersToShow;
        set
        {
            ContainedText.MaxLettersToShow = value;
        }
    }
#endif


    /// <summary>
    /// The maximum number of lines to display. This can be used to
    /// limit how many lines of text are displayed at one time.
    /// </summary>
    public int? MaxNumberOfLines
    {
        get => ContainedText.MaxNumberOfLines;
        set => ContainedText.MaxNumberOfLines = value;
    }

#if RAYLIB
    public Font CustomFont
    {
        get => ContainedText.Font;
        set => ContainedText.Font = value;
    }
#endif

#if !RAYLIB && !SKIA
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

#if RAYLIB || XNALIKE
    /// <summary>
    /// A multiplier used when rendering the text. The default value is 1.0.
    /// </summary>
    /// <remarks>
    /// Setting this value to a value other than 1 scales the text accordingly. This is
    /// a scalue value applied to the existing font, so a value larger than 1 can result
    /// in the font appearing pixellated.
    ///
    /// Since this value does not affect the underlying Font, it can be changed without
    /// requiring a dedicated font asset.
    /// </remarks>
#else
    /// <summary>
    /// A multiplier used when rendering the text. The default value is 1.0.
    /// </summary>
#endif
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

#if !RAYLIB
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
#endif

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
        get => FontFamily;
        set => FontFamily = value;
    }

    /// <summary>
    /// The font name, such as "Arial", which is used to load fonts from
    /// </summary>
    public string FontFamily
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

#if SKIA
    float _boldWeight = 1;
    /// <summary>
    /// Gets or sets the weight multiplier for bold text rendering.
    /// A value of 1.0 represents regular weight, while higher values
    /// increase the thickness of strokes (e.g., 1.5 for bold).
    /// </summary>
    public float BoldWeight
    {
        get => _boldWeight;
        set
        {
            _boldWeight = value;
            ContainedText.BoldWeight = value;
        }
    }

    public bool IsBold
    {
        get => _boldWeight > 1;
        set { BoldWeight = value ? 1.5f : 1f; }
    }
#else
    bool isBold;
    public bool IsBold
    {
        get => isBold;
        set { isBold = value; UpdateToFontValues(); }
    }
#endif

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

#if !RAYLIB
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
#endif

    /// <summary>
    /// Gets or sets the raw text content displayed by the control. This is the value before line wrapping and bbcode parsing has been applied.
    /// </summary>
    /// <remarks>
    /// Setting this property updates the displayed text and may trigger layout changes if the text
    /// size affects the control's dimensions. If the control's width is set relative to its children and no maximum
    /// width is specified, the text will not be line-wrapped.
    /// If a <see cref="Gum.Localization.LocalizationService"/> is registered, the assigned value is passed through
    /// <see cref="Gum.Localization.LocalizationService.Translate"/> before being applied. To bypass translation
    /// (for example, for user-entered text), use <see cref="SetTextNoTranslate"/> instead.
    /// </remarks>
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
                if (this.MaxWidth == null)
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

    /// <summary>
    /// Sets the text without applying localization/translation. Equivalent to calling
    /// <c>SetProperty("TextNoTranslate", value)</c>.
    /// </summary>
    /// <remarks>
    /// This is a method rather than a property because the "no translate" state is not preserved on
    /// the underlying text renderable — only the final string is stored. A corresponding getter would
    /// have no way to distinguish translated from untranslated text, so a property would be misleading.
    /// Use this for text that should not be localized, such as user-entered input in a TextBox.
    /// </remarks>
    public void SetTextNoTranslate(string? value)
    {
        var widthBefore = ContainedText.WrappedTextWidth;
        var heightBefore = ContainedText.WrappedTextHeight;
        if (this.WidthUnits == Gum.DataTypes.DimensionUnitType.RelativeToChildren)
        {
            if (this.MaxWidth == null)
            {
                ContainedText.Width = null;
            }
            else
            {
                ContainedText.Width = this.MaxWidth;
            }
        }

        this.SetProperty("TextNoTranslate", value);

        NotifyPropertyChanged(nameof(Text));
        var shouldUpdate = widthBefore != ContainedText.WrappedTextWidth || heightBefore != ContainedText.WrappedTextHeight;
        if (shouldUpdate)
        {
            UpdateLayout(
                Gum.Wireframe.GraphicalUiElement.ParentUpdateType.IfParentWidthHeightDependOnChildren |
                Gum.Wireframe.GraphicalUiElement.ParentUpdateType.IfParentStacks, int.MaxValue / 2);
        }
    }

    /// <summary>
    /// The lines of text after wrapping and bbcode parsing have been applied.
    /// </summary>
    public IReadOnlyList<string> WrappedText => ContainedText.WrappedText;

#if !RAYLIB
    public OverlapDirection OverlapDirection
    {
        get => ContainedText.OverlapDirection;
        set => ContainedText.OverlapDirection = value;
    }
#endif

    #region Defaults

    // todo - add more here
    public static string DefaultFont = "Arial";
    public static int DefaultFontSize = 18;

    /// <summary>
    /// Indicates whether the font should be assigned during object construction.
    /// </summary>
    /// <remarks>Set this field to <see langword="true"/> to assign the font in the constructor, or to <see
    /// langword="false"/> to defer font assignment until later in the object's lifecycle. This can be set to false
    /// if TextRuntime instances are always given a custom font, so this can prevent unnecessary font loading/assignment.</remarks>
    public static bool AssignFontInConstructor = true;

    /// <summary>
    /// A default BitmapFont to assign to all new TextRuntime instances during construction.
    /// When set, this takes priority over <see cref="DefaultFont"/> and <see cref="DefaultFontSize"/>.
    /// When null, the default font is constructed from <see cref="DefaultFont"/> and <see cref="DefaultFontSize"/>.
    /// </summary>
#if !RAYLIB
    public static BitmapFont? DefaultCustomFont;
#else
    public static Font? DefaultCustomFont;
#endif

    public float DefaultWidth = 0;
    public float DefaultHeight = 0;

    public DimensionUnitType DefaultWidthUnits = DimensionUnitType.RelativeToChildren;
    public DimensionUnitType DefaultHeightUnits = DimensionUnitType.RelativeToChildren;

    #endregion

    public TextRuntime(bool fullInstantiation = true, SystemManagers? systemManagers = null)
    {
        if (fullInstantiation)
        {
            this.SuspendLayout();
            var textRenderable = new Text(systemManagers ?? SystemManagers.Default);
#if !RAYLIB
            textRenderable.RenderBoundary = false;
#endif
            mContainedText = textRenderable;

            SetContainedObject(textRenderable);

            Width = DefaultWidth;
            WidthUnits = DefaultWidthUnits;
            Height = DefaultHeight;
            HeightUnits = DefaultHeightUnits;
            if(AssignFontInConstructor)
            {
                if(DefaultCustomFont != null)
                {
#if !RAYLIB
                    this.BitmapFont = DefaultCustomFont;
#else
                    this.CustomFont = DefaultCustomFont.Value;
#endif
                }
                else
                {
                    this.FontSize = DefaultFontSize;
                    this.Font = DefaultFont;
                }
            }
            HasEvents = false;

            textRenderable.RawText = "Hello World";
            this.ResumeLayout();
        }
    }

#if !RAYLIB && !SKIA
    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. myText.AddToRoot()).")]
    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);
#endif

#if !RAYLIB
    /// <summary>
    /// Returns the index of the character at the specified screen position. This returns the index
    /// within the WrappedText, so to index in, you need to loop through each line.
    /// </summary>
    /// <param name="screenX">The screen x position, usually obtained by Cursor.XRespectingGumZoomAndBounds()</param>
    /// <param name="screenY">The screen y position, usually obtained by Cursor.YRespectingGumZoomAndBounds()</param>
    /// <returns>The index in the WrappedText</returns>
    public int GetCharacterIndexAtPosition(float screenX, float screenY) => ContainedText.GetCharacterIndexAtPosition(screenX, screenY);
#endif
}
