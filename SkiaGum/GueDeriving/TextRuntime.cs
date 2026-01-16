using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace SkiaGum.GueDeriving;

public class TextRuntime : BindableGue
{

    public enum ColorCategory
    {
        White,
        DefaultColor,
        LightBlue,
        LightGray
    }

    ColorCategory mColorCategoryState;
    public ColorCategory ColorCategoryState
    {
        get => mColorCategoryState;
        set
        {
            if (mColorCategoryState == value) return;
            mColorCategoryState = value;

            // Assign the entire color at once
            Color = value switch
            {
                ColorCategory.White => new SKColor(255, 255, 255),
                ColorCategory.DefaultColor => new SKColor(69, 90, 100),
                ColorCategory.LightBlue => new SKColor(0, 145, 193),
                ColorCategory.LightGray => new SKColor(226, 226, 227),
                _ => Color // No change for unknown categories
            };
        }
    }

    private TextOverflowHorizontalMode textOverflowHorizontalMode;
    public TextOverflowHorizontalMode TextOverflowHorizontalMode
    {
        get => textOverflowHorizontalMode;
        set
        {
            textOverflowHorizontalMode = value;
            GetContainedText().IsTruncatingWithEllipsisOnLastLine =
                        (value == TextOverflowHorizontalMode.EllipsisLetter);
        }
    }

    private Text? mContainedText;

    private Text GetContainedText()
    {
        if (RenderableComponent is not Text text)
            throw new InvalidOperationException($"Expected RenderableComponent to be Text but was {RenderableComponent?.GetType().Name ?? "null"}.");

        if (!ReferenceEquals(mContainedText, text))
            mContainedText = text;

        return text;
    }

    public string Text { get => GetContainedText().RawText; set => GetContainedText().RawText = value; }
    public SKColor Color { get => GetContainedText().Color; set => GetContainedText().Color = value; }
    public float FontScale { get => GetContainedText().FontScale; set => GetContainedText().FontScale = value; }
    public bool IsBold { get => GetContainedText().BoldWeight > 1; set => GetContainedText().BoldWeight = value ? 1.5f : 1f; }
    public float BoldWeight { get => GetContainedText().BoldWeight; set => GetContainedText().BoldWeight = value; }
    public HorizontalAlignment HorizontalAlignment { get => GetContainedText().HorizontalAlignment; set => GetContainedText().HorizontalAlignment = value; }
    public VerticalAlignment VerticalAlignment { get => GetContainedText().VerticalAlignment; set => GetContainedText().VerticalAlignment = value; }

    //public SKTypeface FontType
    //{
    //    get => ContainedText.Font;
    //    set => ContainedText.Font = value;
    //}

    public int FontSize
    {
        get => GetContainedText().FontSize;
        set
        {
            var text = GetContainedText();
            if (text.FontSize == value) return; // Optimization: Only update if changed
            text.FontSize = value;
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
    public bool UseCustomFont { get => useCustomFont; set => SetFontProperty(ref useCustomFont, value); }

    string customFontFile = string.Empty;
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

    string font = string.Empty;
    /// <summary>
    /// The font name, such as "Arial", which is used to load fonts from 
    /// </summary>
    public string Font { get => font; set => SetFontProperty(ref font, value); }

    private bool isItalic;
    public bool IsItalic { get => isItalic; set => SetFontProperty(ref isItalic, value); }
    

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

    public TextRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new Text());
            ApplyDefaultGumState();
        }
    }

    private void ApplyDefaultGumState()
    {
        Height = 0;
        HeightUnits = DimensionUnitType.RelativeToChildren;
        Width = 0;
        WidthUnits = DimensionUnitType.RelativeToChildren;

        FontSize = 18;
        Text = "Hello";

        // Using simplified Color property
        Color = SKColors.White;
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (TextRuntime)base.Clone();

        toReturn.mContainedText = null;

        return toReturn;
    }

    private void SetFontProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        UpdateToFontValues();
    }
}
