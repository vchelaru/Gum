using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.Graphics.Animation;
using Gum.RenderingLibrary;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilitiesStandard.Helpers;
using System.Net;
using System.IO;
using MonoGameGum.Localization;
using System.Security.Policy;
using Gum.Managers;
using Microsoft.Xna.Framework.Graphics;

#if GUM
using Gum.Services;

#endif





#if GUM
using Gum.ToolStates;
#endif
namespace Gum.Wireframe;

public class CustomSetPropertyOnRenderable
{
    public static ILocalizationService LocalizationService { get; set; }
#if GUM
    private static readonly FontManager _fontManager;
#endif

    static CustomSetPropertyOnRenderable()
    {
#if GUM
        _fontManager = Builder.Get<FontManager>();
#endif
    }

    public static void SetPropertyOnRenderable(IRenderableIpso renderableIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;

        // First try special-casing.  

        if (renderableIpso is Text)
        {
            handled = TrySetPropertyOnText(renderableIpso, graphicalUiElement, propertyName, value);
        }
#if MONOGAME || KNI || XNA4 || FNA
        else if (renderableIpso is LineCircle)
        {
            handled = TrySetPropertyOnLineCircle(renderableIpso, graphicalUiElement, propertyName, value);
        }
        else if (renderableIpso is LineRectangle)
        {
            handled = TrySetPropertyOnLineRectangle(renderableIpso, graphicalUiElement, propertyName, value);
        }
        else if (renderableIpso is LinePolygon)
        {
            handled = TrySetPropertyOnLinePolygon(renderableIpso, propertyName, value);
        }
        else if (renderableIpso is SolidRectangle)
        {
            var solidRect = renderableIpso as SolidRectangle;

            if (propertyName == "Blend")
            {
                var valueAsGumBlend = (RenderingLibrary.Blend)value;

                var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

                solidRect.BlendState = valueAsXnaBlend;

                handled = true;
            }
            else if (propertyName == "Alpha")
            {
                int valueAsInt = (int)value;
                solidRect.Alpha = valueAsInt;
                handled = true;
            }
            else if (propertyName == "Red")
            {
                int valueAsInt = (int)value;
                solidRect.Red = valueAsInt;
                handled = true;
            }
            else if (propertyName == "Green")
            {
                int valueAsInt = (int)value;
                solidRect.Green = valueAsInt;
                handled = true;
            }
            else if (propertyName == "Blue")
            {
                int valueAsInt = (int)value;
                solidRect.Blue = valueAsInt;
                handled = true;
            }
            else if (propertyName == "Color")
            {
                //var valueAsColor = (Color)value;
                if (value is System.Drawing.Color drawingColor)
                {
                    solidRect.Color = drawingColor;
                }
                else if (value is Microsoft.Xna.Framework.Color xnaColor)
                {
                    solidRect.Color = xnaColor.ToSystemDrawing();

                }

                handled = true;
            }

        }
        else if (renderableIpso is Sprite)
        {
            handled = TrySetPropertyOnSprite(renderableIpso, graphicalUiElement, propertyName, value);
        }
        else if (renderableIpso is NineSlice)
        {
            handled = TrySetPropertyOnNineSlice(renderableIpso, graphicalUiElement, propertyName, value, handled);
        }
        else if (renderableIpso is InvisibleRenderable)
        {
            handled = TrySetPropertyOnInvisbileRenderable(renderableIpso, propertyName, value, handled);
        }
#endif

        // If special case didn't work, let's try reflection
        if (!handled)
        {
            if (propertyName == "Parent")
            {
                // do something
            }
            else
            {
                System.Reflection.PropertyInfo propertyInfo = renderableIpso.GetType().GetProperty(propertyName);

                if (propertyInfo != null && propertyInfo.CanWrite)
                {

                    if (value.GetType() != propertyInfo.PropertyType)
                    {
                        value = System.Convert.ChangeType(value, propertyInfo.PropertyType);
                    }
                    propertyInfo.SetValue(renderableIpso, value, null);
                }
            }
        }
    }

    private static bool TrySetPropertyOnInvisbileRenderable(IRenderableIpso renderableIpso, string propertyName, object value, bool handled)
    {
        bool didSet = false;
        switch (propertyName)
        {
            case "IsRenderTarget":
                (renderableIpso as InvisibleRenderable).IsRenderTarget = value as bool? ?? false;
                didSet = true;
                break;
            case "Alpha":
                if(value is int asInt)
                {
                    (renderableIpso as InvisibleRenderable).Alpha = asInt;
                }
                else
                {
                    (renderableIpso as InvisibleRenderable).Alpha = value as float? ?? 255;
                }
                didSet = true;
                break;
        }

        return didSet;
    }

    private static bool TrySetPropertyOnNineSlice(IRenderableIpso renderableIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value, bool handled)
    {
        var nineSlice = renderableIpso as NineSlice;

        if (propertyName == "SourceFile")
        {
            AssignSourceFileOnNineSlice(value as string, graphicalUiElement, nineSlice);
            handled = true;
        }
        else if (propertyName == "Blend")
        {
            var valueAsGumBlend = (RenderingLibrary.Blend)value;

            var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

            nineSlice.BlendState = valueAsXnaBlend;

            handled = true;
        }
        else if (propertyName == nameof(NineSlice.CustomFrameTextureCoordinateWidth))
        {
            var asFloat = value as float?;

            nineSlice.CustomFrameTextureCoordinateWidth = asFloat;

            handled = true;
        }
        else if (propertyName == "Color")
        {
            if (value is System.Drawing.Color drawingColor)
            {
                nineSlice.Color = drawingColor;
            }
            else if (value is Microsoft.Xna.Framework.Color xnaColor)
            {
                nineSlice.Color = xnaColor.ToSystemDrawing();

            }
            handled = true;
        }
        else if(propertyName == "Red")
        {
            nineSlice.Red = (int)value;
            handled = true;
        }
        else if (propertyName == "Green")
        {
            nineSlice.Green = (int)value;
            handled = true;
        }
        else if (propertyName == "Blue")
        {
            nineSlice.Blue = (int)value;
            handled = true;
        }
        else if (propertyName == "Texture")
        {
            nineSlice.SetSingleTexture((Texture2D)value);
            handled = true;
        }

        // Texture coordiantes like TextureLeft, TextureRight, TextureWidth, and TextureHeight
        // are handled by GraphicalUiElement so we don't have to handle it here

        return handled;
    }

    private static void AssignSourceFileOnNineSlice(string value, GraphicalUiElement graphicalUiElement, NineSlice nineSlice)
    {
        var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;

        if (string.IsNullOrEmpty(value))
        {
            nineSlice.SetSingleTexture(null);
        }
        else if (value.EndsWith(".achx"))
        {
            AnimationChainList animationChainList = GetAnimationChainList(ref value, loaderManager);

            nineSlice.AnimationChains = animationChainList;

            nineSlice.RefreshCurrentChainToDesiredName();

            nineSlice.UpdateToCurrentAnimationFrame();

            graphicalUiElement.UpdateTextureValuesFrom(nineSlice);

        }
        else
        {
            if (ToolsUtilities.FileManager.IsRelative(value))
            {
                value = ToolsUtilities.FileManager.RelativeDirectory + value;
                value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
            }

            //check if part of atlas
            //Note: assumes that if this filename is in an atlas that all 9 are in an atlas
            var atlasedTexture = global::RenderingLibrary.Content.LoaderManager.Self.TryLoadContent<AtlasedTexture>(value);
            if (atlasedTexture != null)
            {
                nineSlice.LoadAtlasedTexture(value, atlasedTexture);
            }
            else
            {
                if (NineSliceExtensions.GetIfShouldUsePattern(value))
                {
                    nineSlice.SetTexturesUsingPattern(value, SystemManagers.Default, false);
                }
                else
                {

                    Microsoft.Xna.Framework.Graphics.Texture2D? texture =
                        Sprite.InvalidTexture;

                    try
                    {
                        texture =
                            loaderManager.LoadContent<Microsoft.Xna.Framework.Graphics.Texture2D>(value);
                    }
                    catch (Exception e)
                    {
                        if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
                        {
                            string message = $"Error setting SourceFile on NineSlice named {nineSlice.Name}:\n{value}";
                            throw new System.IO.FileNotFoundException(message);
                        }
                        // do nothing?
                    }
                    nineSlice.SetSingleTexture(texture);

                }
            }
        }
    }

    private static bool TrySetPropertyOnSprite(IRenderableIpso renderableIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;
        var sprite = renderableIpso as Sprite;

        if (propertyName == "SourceFile")
        {
            var asString = value as String;
            handled = AssignSourceFileOnSprite(sprite, graphicalUiElement, asString);

        }
        else if (propertyName == nameof(Sprite.Alpha))
        {
            int valueAsInt = (int)value;
            sprite.Alpha = valueAsInt;
            handled = true;
        }
        else if (propertyName == nameof(Sprite.Red))
        {
            int valueAsInt = (int)value;
            sprite.Red = valueAsInt;
            handled = true;
        }
        else if (propertyName == nameof(Sprite.Green))
        {
            int valueAsInt = (int)value;
            sprite.Green = valueAsInt;
            handled = true;
        }
        else if (propertyName == nameof(Sprite.Blue))
        {
            int valueAsInt = (int)value;
            sprite.Blue = valueAsInt;
            handled = true;
        }
        else if (propertyName == nameof(Sprite.Color))
        {
            if (value is System.Drawing.Color drawingColor)
            {
                sprite.Color = drawingColor;
            }
            else if (value is Microsoft.Xna.Framework.Color xnaColor)
            {
                sprite.Color = xnaColor.ToSystemDrawing();

            }
            handled = true;
        }

        else if (propertyName == "Blend")
        {
            var valueAsGumBlend = (RenderingLibrary.Blend)value;

            var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

            sprite.BlendState = valueAsXnaBlend;

            handled = true;
        }
        else if (propertyName == nameof(Sprite.Animate))
        {
            sprite.Animate = (bool)value;
            handled = true;
        }
        else if (propertyName == nameof(Sprite.CurrentChainName))
        {
            sprite.CurrentChainName = (string)value;
            graphicalUiElement.UpdateTextureValuesFrom(sprite);
            graphicalUiElement.UpdateLayout();
            handled = true;
        }

        return handled;
    }

    #region Text

    public static bool TrySetPropertyOnText(IRenderableIpso mContainedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;

#if FRB
        // FRB doesn't yet have a TextRuntime, so we have to do this:
        var textRuntime = graphicalUiElement;
#else
        var textRuntime = graphicalUiElement as MonoGameGum.GueDeriving.TextRuntime;
#endif

        void ReactToFontValueChange()
        {
            UpdateToFontValues(mContainedObjectAsIpso as IText, graphicalUiElement);

            handled = true;
        }

        if (propertyName == "Text" || propertyName == "TextNoTranslate")
        {
            var asText = ((Text)mContainedObjectAsIpso);
            if (graphicalUiElement.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                graphicalUiElement.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                // make it have no line wrap width before assignign the text:
                asText.Width = null;
            }

            var valueAsString = value as string;


            asText.InlineVariables.Clear();
            if (valueAsString?.Contains("[") == true)
            {

                // todo - eventually support localization here:
                asText.StoredMarkupText = valueAsString;
                SetBbCodeText(asText, graphicalUiElement, asText.StoredMarkupText);
            }
            else
            {
                asText.StoredMarkupText = null;
                var rawText = valueAsString;
                if(LocalizationService != null && propertyName == "Text")
                {
                    rawText = LocalizationService.Translate(rawText);
                }
                asText.RawText = rawText;
            }
            // we want to update if the text's size is based on its "children" (the letters it contains)
            if (graphicalUiElement.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                graphicalUiElement.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                graphicalUiElement.UpdateLayout();
            }
            handled = true;
        }
        else if (propertyName == "Font Scale" || propertyName == "FontScale")
        {
            ((Text)mContainedObjectAsIpso).FontScale = (float)value;
            // we want to update if the text's size is based on its "children" (the letters it contains)
            if (graphicalUiElement.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                graphicalUiElement.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                graphicalUiElement.UpdateLayout();
            }
            handled = true;

        }
        else if (propertyName == "Font")
        {
            if(textRuntime != null)
            {
                textRuntime.Font = value as string;
            }

            ReactToFontValueChange();
        }
#if MONOGAME || KNI || XNA4 || FNA
        else if (propertyName == nameof(textRuntime.UseCustomFont))
        {
            if (textRuntime != null)
            {
                textRuntime.UseCustomFont = (bool)value;
            }
            var asText = ((Text)mContainedObjectAsIpso);

            if (!string.IsNullOrEmpty(asText.StoredMarkupText))
            {
                SetBbCodeText(asText, graphicalUiElement, asText.StoredMarkupText);
            }
            ReactToFontValueChange();
        }

        else if (propertyName == nameof(textRuntime.CustomFontFile))
        {
            if (textRuntime != null)
            {
                textRuntime.CustomFontFile = (string)value;
            }
            ReactToFontValueChange();

        }
#if USE_GUMCOMMON
        else if(propertyName == nameof(MonoGameGum.GueDeriving.TextRuntime.BitmapFont))
        {
            if(textRuntime != null)
            {
                textRuntime.BitmapFont = (BitmapFont)value;
            }
            handled = true;
        }
#endif
#endif
        else if (propertyName == nameof(textRuntime.FontSize))
        {
            if (textRuntime != null)
            {
                textRuntime.FontSize = (int)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(textRuntime.OutlineThickness))
        {
            if (textRuntime != null)
            {
                textRuntime.OutlineThickness = (int)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(textRuntime.IsItalic))
        {
            if (textRuntime != null)
            {
                textRuntime.IsItalic = (bool)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(textRuntime.IsBold))
        {
            if (textRuntime != null)
            {
                textRuntime.IsBold = (bool)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == "LineHeightMultiplier")
        {
            var asText = ((Text)mContainedObjectAsIpso);
            asText.LineHeightMultiplier = (float)value;
        }
        else if (propertyName == nameof(textRuntime.UseFontSmoothing))
        {
            if (textRuntime != null)
            {
                textRuntime.UseFontSmoothing = (bool)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(Blend))
        {
#if MONOGAME || KNI || XNA4 || FNA
            var valueAsGumBlend = (RenderingLibrary.Blend)value;

            var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

            var text = mContainedObjectAsIpso as Text;
            text.BlendState = valueAsXnaBlend;
            handled = true;
#endif
        }
        else if (propertyName == "Alpha")
        {
#if MONOGAME || KNI || XNA4 || FNA
            int valueAsInt = (int)value;
            ((Text)mContainedObjectAsIpso).Alpha = valueAsInt;
            handled = true;
#endif
        }
        else if (propertyName == "Red")
        {
            int valueAsInt = (int)value;
            ((Text)mContainedObjectAsIpso).Red = valueAsInt;
            handled = true;
        }
        else if (propertyName == "Green")
        {
            int valueAsInt = (int)value;
            ((Text)mContainedObjectAsIpso).Green = valueAsInt;
            handled = true;
        }
        else if (propertyName == "Blue")
        {
            int valueAsInt = (int)value;
            ((Text)mContainedObjectAsIpso).Blue = valueAsInt;
            handled = true;
        }
        else if (propertyName == "Color")
        {
#if MONOGAME || KNI || XNA4 || FNA
            //var valueAsColor = (Color)value;
            //((Text)mContainedObjectAsIpso).Color = valueAsColor;
            //handled = true;
            if (value is System.Drawing.Color drawingColor)
            {
                ((Text)mContainedObjectAsIpso).Color = drawingColor;
                handled = true;
            }
            else if (value is Microsoft.Xna.Framework.Color xnaColor)
            {
                ((Text)mContainedObjectAsIpso).Color = xnaColor.ToSystemDrawing();
                handled = true;
            }
#endif
        }

        else if (propertyName == "HorizontalAlignment")
        {
            ((Text)mContainedObjectAsIpso).HorizontalAlignment = (HorizontalAlignment)value;
            handled = true;
        }
        else if (propertyName == "VerticalAlignment")
        {
            ((Text)mContainedObjectAsIpso).VerticalAlignment = (VerticalAlignment)value;
            handled = true;
        }
        else if (propertyName == "MaxLettersToShow")
        {
#if MONOGAME || KNI || XNA4 || FNA
            ((Text)mContainedObjectAsIpso).MaxLettersToShow = (int?)value;
            handled = true;
#endif
        }
        else if (propertyName == "MaxNumberOfLines")
        {
            ((Text)mContainedObjectAsIpso).MaxNumberOfLines = (int?)value;
            handled = true;
        }

        else if (propertyName == nameof(TextOverflowHorizontalMode))
        {
            var textOverflowMode = (TextOverflowHorizontalMode)value;

            if (textOverflowMode == TextOverflowHorizontalMode.EllipsisLetter)
            {
                ((Text)mContainedObjectAsIpso).IsTruncatingWithEllipsisOnLastLine = true;
            }
            else
            {
                ((Text)mContainedObjectAsIpso).IsTruncatingWithEllipsisOnLastLine = false;
            }
        }
        else if (propertyName == nameof(TextOverflowVerticalMode))
        {
            graphicalUiElement.TextOverflowVerticalMode = (TextOverflowVerticalMode)value;
            graphicalUiElement.RefreshTextOverflowVerticalMode();

        }

        return handled;
    }

    // For some reason this crashes on web when uploading to itch:
    //public static HashSet<string> Tags { get; private set; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
    // OrdinalIgnoreCase works fine:
    public static HashSet<string> Tags { get; private set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "alpha",
        "red",
        "blue",
        "green",
        "color",
        "font",
        "fontsize",
        "outlinethickness",
        "isitalic",
        "isbold",
        "usefontsmoothing",
        "fontscale",
        "lineheightmultiplier"

    };

    static Stack<int> fontSizeStack = new Stack<int>();
    static Stack<string> fontNameStack = new Stack<string>();
    static Stack<int> outlineThicknessStack = new Stack<int>();
    static Stack<bool> useFontSmoothingStack = new Stack<bool>();
    static Stack<bool> isItalicStack = new Stack<bool>();
    static Stack<bool> isBoldStack = new Stack<bool>();
    static Stack<bool> useCustomFontStack = new Stack<bool>();

    static List<TagInfo> allTags = new List<TagInfo>();

    private static void SetBbCodeText(global::RenderingLibrary.Graphics.Text asText, GraphicalUiElement graphicalUiElement, string bbcode)
    {
        // Text can be rendered on multiple lines. This can happen due to explicit newline characters, or by automatic line wrapping.
        // When line indexes are counted, newlines are not included. Therefore, we need to remove newlines here so that indexes match up.
        var bbCodeNoNewlines =
        // update November 18, 2024
        // We now do include newline
        // characters becuase those can
        // be explicitly added for textboxes
        // with multiple lines:
        //bbcode?.Replace("\n", "");
        bbcode;

        var resultsNoNewlines = BbCodeParser.Parse(bbCodeNoNewlines, Tags);
        var resultsWithNewlines = BbCodeParser.Parse(bbcode, Tags);

        var strippedText = BbCodeParser.RemoveTags(bbcode, resultsWithNewlines);
        asText.RawText = strippedText;

        fontNameStack.Clear();

#if FRB
        var textRuntime = graphicalUiElement;
#else
        var textRuntime = graphicalUiElement as MonoGameGum.GueDeriving.TextRuntime;
#endif

        if(textRuntime != null)
        {
            if (textRuntime.UseCustomFont)
            {
                var customFont = textRuntime.CustomFontFile;
                if (customFont?.EndsWith(".fnt") == true)
                {
                    customFont = customFont.Substring(0, customFont.Length - ".fnt".Length);
                }
                fontNameStack.Push(customFont);
            }
            else
            {
                fontNameStack.Push(textRuntime.Font);
            }

            fontSizeStack.Clear();
            fontSizeStack.Push(textRuntime.FontSize);

            outlineThicknessStack.Clear();
            outlineThicknessStack.Push(textRuntime.OutlineThickness);

            useFontSmoothingStack.Clear();
            useFontSmoothingStack.Push(textRuntime.UseFontSmoothing);

            isItalicStack.Clear();
            isItalicStack.Push(textRuntime.IsItalic);

            isBoldStack.Clear();
            isBoldStack.Push(textRuntime.IsBold);

            useCustomFontStack.Clear();
            useCustomFontStack.Push(textRuntime.UseCustomFont);

        }

        var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
        var contentLoader = loaderManager.ContentLoader;

        foreach (var item in resultsNoNewlines)
        {
            object castedValue = item.Open.Argument;
            var shouldApply = false;
            switch (item.Name)
            {
                case "Red":
                case "Green":
                case "Blue":
                    castedValue = byte.Parse(item.Open.Argument);
                    shouldApply = true;
                    break;
                case "Color":
                    {
                        int result;

                        if (item.Open.Argument?.StartsWith("0x") == true && int.TryParse(item.Open.Argument.Substring(2),
                                                                            NumberStyles.AllowHexSpecifier,
                                                                            null,
                                                                            out result))
                        {
                            castedValue = result;
                            castedValue = System.Drawing.Color.FromArgb(result);
                        }
                        else
                        {
                            castedValue = System.Drawing.Color.FromName(item.Open.Argument);
                        }
                        shouldApply = true;
                    }
                    break;
                case "FontScale":
                    {
                        if (float.TryParse(item.Open.Argument, out float parsed))
                        {
                            castedValue = parsed;
                            shouldApply = true;
                        }
                    }
                    break;

                    // Don't do anything like IsBold or IsItalic here - these are handled in ApplyFontVariables
            }

            if (shouldApply)
            {
                var inlineVariable = new InlineVariable
                {
                    CharacterCount = item.Close.StartStrippedIndex - item.Open.StartStrippedIndex,
                    StartIndex = item.Open.StartStrippedIndex,
                    VariableName = item.Name,
                    Value = castedValue
                };

                asText.InlineVariables.Add(inlineVariable);
            }
        }

        ApplyFontVariables(asText, resultsNoNewlines);
    }

    private static void ApplyFontVariables(Text asText, List<FoundTag> results)
    {
        allTags.Clear();
        allTags.AddRange(results.Select(item => item.Open));
        allTags.AddRange(results.Select(item => item.Close));
        allTags.Sort((a, b) => a.StartIndex - b.StartIndex);

        InlineVariable lastFontInlineVariable = null;
        foreach (var tag in allTags)
        {

            BitmapFont castedValue = null;
            string convertedName = "BitmapFont";
            var hasArg = !string.IsNullOrEmpty(tag.Argument);
            switch (tag.Name)
            {
                case "Font":
                    {
                        if (hasArg)
                        {
                            // tolerate ".fnt" suffix
                            var argument = tag.Argument;
                            if (argument?.EndsWith(".fnt") == true)
                            {
                                argument = argument.Substring(0, argument.Length - ".fnt".Length);
                            }
                            fontNameStack.Push(argument);
                            castedValue = GetAndCreateFontIfNecessary();
                        }
                        else
                        {
                            fontNameStack.Pop();
                            castedValue = GetAndCreateFontIfNecessary();
                        }
                    }
                    break;
                case "FontSize":
                    {
                        if (int.TryParse(tag.Argument, out int parsedValue))
                        {
                            fontSizeStack.Push(parsedValue);
                            castedValue = GetAndCreateFontIfNecessary();
                        }
                        else
                        {
                            fontSizeStack.Pop();
                            castedValue = GetAndCreateFontIfNecessary();
                        }
                    }
                    break;
                case "OutlineThickness":
                    {
                        if (int.TryParse(tag.Argument, out int parsedValue))
                        {
                            outlineThicknessStack.Push(parsedValue);
                            castedValue = GetAndCreateFontIfNecessary();
                        }
                        else
                        {
                            outlineThicknessStack.Pop();
                            castedValue = GetAndCreateFontIfNecessary();
                        }
                    }
                    break;
                case "IsItalic":
                    {
                        if (bool.TryParse(tag.Argument, out bool parsedValue))
                        {
                            isItalicStack.Push(parsedValue);
                            castedValue = GetAndCreateFontIfNecessary();
                        }
                        else
                        {
                            isItalicStack.Pop();
                            castedValue = GetAndCreateFontIfNecessary();
                        }
                    }
                    break;
                case "IsBold":
                    {
                        if (bool.TryParse(tag.Argument, out bool parsedValue))
                        {
                            isBoldStack.Push(parsedValue);
                            castedValue = GetAndCreateFontIfNecessary();
                        }
                        else
                        {
                            isBoldStack.Pop();
                            castedValue = GetAndCreateFontIfNecessary();
                        }
                    }
                    break;
                case "UseCustomFont":
                    {
                        if (bool.TryParse(tag.Argument, out bool parsedValue))
                        {
                            useCustomFontStack.Push(parsedValue);
                            castedValue = GetAndCreateFontIfNecessary();
                        }
                        else
                        {
                            useCustomFontStack.Pop();
                            castedValue = GetAndCreateFontIfNecessary();
                        }
                    }
                    break;

            }

            if (castedValue != null)
            {
                if (lastFontInlineVariable != null)
                {
                    lastFontInlineVariable.CharacterCount = tag.StartStrippedIndex - lastFontInlineVariable.StartIndex;
                }

                var inlineVariable = new InlineVariable
                {
                    // assigned above:
                    //CharacterCount = tag.Close.StartStrippedIndex - tag.Open.StartStrippedIndex,
                    StartIndex = tag.StartStrippedIndex,
                    VariableName = convertedName,
                    Value = castedValue
                };

                asText.InlineVariables.Add(inlineVariable);

                lastFontInlineVariable = inlineVariable;
            }
        }

        // close off the last one:
        if (lastFontInlineVariable != null)
        {
            lastFontInlineVariable.CharacterCount = asText.RawText.Length - lastFontInlineVariable.StartIndex;
        }


        BitmapFont GetAndCreateFontIfNecessary()
        {
            var fontFileName = GetFontFileName();

            var font = global::RenderingLibrary.Content.LoaderManager.Self.GetDisposable(fontFileName) as BitmapFont;

            if (font == null)
            {
                font = GetFontDisposable(fontFileName);
            }

            // no cache, does it need to be created?
            if (font == null)
            {
                // this could be a custom font, so let's see if it exists:

                string fileName = String.Empty;
                if (ToolsUtilities.FileManager.FileExists(fontFileName))
                {
                    fileName = fontFileName;
                }
                else
                {
#if GUM
                    fileName = _fontManager.AbsoluteFontCacheFolder +
                        ToolsUtilities.FileManager.RemovePath(fontFileName);
#endif
                }

#if GUM

                if (!ToolsUtilities.FileManager.FileExists(fileName))
                {
                    // user could have typed anything in there, so who knows if this will succeed. Therefore, try/catch:
                    try
                    {
                        BmfcSave.CreateBitmapFontFilesIfNecessary(
                            fontSizeStack.Peek(),
                            fontNameStack.Peek(),
                            outlineThicknessStack.Peek(),
                            useFontSmoothingStack.Peek(),
                            isItalicStack.Peek(),
                            isBoldStack.Peek(),
                            GumState.Self.ProjectState.GumProjectSave?.FontRanges,
                            GumState.Self.ProjectState.GumProjectSave?.FontSpacingHorizontal ?? 1,
                            GumState.Self.ProjectState.GumProjectSave?.FontSpacingVertical ?? 1

                            );
                    }
                    catch
                    {
                        // do nothing?
                    }
                }
#endif

                if (ToolsUtilities.FileManager.FileExists(fileName))
                {
                    font = new BitmapFont(fileName);
                }
                else
                {
                    // This can happen when closing tags are encountered at the end of a font. If no font exists, we can just go to the default
                    font = Text.DefaultBitmapFont;
                }
                global::RenderingLibrary.Content.LoaderManager.Self.AddDisposable(fontFileName, font);
            }

            return font;
        }

        string GetFontFileName()
        {
            string fontFileNameName;
            if (useCustomFontStack.Peek())
            {
                fontFileNameName = fontNameStack.Peek() + ".fnt";
            }
            else
            {
                fontFileNameName = global::RenderingLibrary.Graphics.Fonts.BmfcSave.GetFontCacheFileNameFor(
                    fontSizeStack.Peek(),
                    fontNameStack.Peek(),
                    outlineThicknessStack.Peek(),
                    useFontSmoothingStack.Peek(),
                    isItalicStack.Peek(),
                    isBoldStack.Peek());

            }

            var fullFileName = ToolsUtilities.FileManager.RemoveDotDotSlash(ToolsUtilities.FileManager.Standardize(fontFileNameName, false, true));
#if ANDROID || IOS
            fullFileName = fullFileName.ToLowerInvariant();
#endif
            return fullFileName;
        }
    }

    public static void UpdateToFontValues(IText text, GraphicalUiElement graphicalUiElement)
    {
        // January 28, 2025
        // If we early-out here,
        // then the bitmap values
        // never get assigned. This
        // means that eventually when
        // layout is resumed, bitmap values
        // will get assigned. However, assigning
        // bitmap values on a Text that has has Width
        // or Height Units of Relative to Children, the
        // parent then updates its parents layout. This causes
        // tons of layout calls when resuming layout on a list box.
        // Instead, we should assign fonts and mark font as dirty, then
        // on resume only the parent layout needs to happen.
        //if (graphicalUiElement.IsLayoutSuspended || GraphicalUiElement.IsAllLayoutSuspended)
        //{
        //    graphicalUiElement.IsFontDirty = true;
        //}
        // todo: This could make things faster, but it will require
        // extra calls in generated code, or an "UpdateAll" method
        //if (!mIsLayoutSuspended && !IsAllLayoutSuspended)

        // Residual properties could exist on a Text instnace, so we need to
        // tolerate a missing item and not crash. 

#if FRB
        // FRB doesn't yet have a TextRuntime, so we have to do this:
        var textRuntime = graphicalUiElement;
#else
        var textRuntime = graphicalUiElement as MonoGameGum.GueDeriving.TextRuntime;

#endif
        if (text == null || textRuntime == null)
        {
            return;
        }

        BitmapFont font = null;

        var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
        var contentLoader = loaderManager.ContentLoader;

        if (textRuntime.UseCustomFont)
        {

            if (!string.IsNullOrEmpty(textRuntime.CustomFontFile))
            {
                font = loaderManager.GetDisposable(textRuntime.CustomFontFile) as BitmapFont;
                if (font == null)
                {
#if KNI
                        try
                        {
                            // this could be running in browser where we don't have File.Exists, so JUST DO IT
                            font = new BitmapFont(textRuntime.CustomFontFile);
                            loaderManager.AddDisposable(textRuntime.CustomFontFile, font);
                        }
                        catch
                        {
                            // font doesn't exist, carry on...
                        }
#else
                    // so normally we would just let the content loader check if the file exists but since we're not going to
                    // use the content loader for BitmapFont, we're going to protect this with a file.exists.
                    if (ToolsUtilities.FileManager.FileExists(textRuntime.CustomFontFile))
                    {
                        font = new BitmapFont(textRuntime.CustomFontFile);
                        loaderManager.AddDisposable(textRuntime.CustomFontFile, font);
                    }
#endif
                }
                else if (font.Textures.Any(item => item?.IsDisposed == true))
                {
                    // The BitmapFont is cached by Gum, but the underlying Texture2D might be managed by something else (like FRB).
                    // This means that the Texture can be disposed without the BitmapFont being disposed. If this is the case we should
                    // ask the underlying system for a new .png, but we can keep the same BitmapFont since that should stay the same and
                    // .fnt parsing can be the slow part for large fonts.
                    font.ReAssignTextures();
                }
            }


        }
        else
        {
            if (textRuntime.FontSize > 0 && !string.IsNullOrEmpty(textRuntime.Font))
            {

                string fontName = global::RenderingLibrary.Graphics.Fonts.BmfcSave.GetFontCacheFileNameFor(
                    textRuntime.FontSize,
                    textRuntime.Font,
                    textRuntime.OutlineThickness,
                    textRuntime.UseFontSmoothing,
                    textRuntime.IsItalic,
                    textRuntime.IsBold);

                string fullFileName = ToolsUtilities.FileManager.Standardize(fontName, preserveCase: true, makeAbsolute: true);

                font = loaderManager.GetDisposable(fullFileName) as BitmapFont;

                // Attempt to load from Embedded Resource

                if (fontName != null && font == null)
                {
                    font = GetFontDisposable(fontName);
                }

                if (font == null || font.Texture?.IsDisposed == true)
                {
#if KNI
                        try
                        {
                            // this could be running in browser where we don't have File.Exists, so JUST DO IT
                            font = new BitmapFont(fullFileName);

                            loaderManager.AddDisposable(fullFileName, font);
                        }
                        catch
                        {
                            // font doesn't exist, carry on...
                        }
#else
                    // so normally we would just let the content loader check if the file exists but since we're not going to
                    // use the content loader for BitmapFont, we're going to protect this with a file.exists.
                    if (ToolsUtilities.FileManager.FileExists(fullFileName))
                    {
                        // kill the old font:
                        if(font?.Texture?.IsDisposed == true)
                        {
                            loaderManager.Dispose(fullFileName);
                        }

                        font = new BitmapFont(fullFileName);


                        loaderManager.AddDisposable(fullFileName, font);
                    }
#endif
                }

                // FRB may dispose fonts, so let's check:

#if DEBUG
                if (font?.Textures.Any(item => item?.IsDisposed == true) == true)
                {
                    throw new InvalidOperationException("The returned font has a disposed texture");
                }
#endif
            }
        }

        var fontToSet = font ?? Text.DefaultBitmapFont;

        var asRenderableText = (Text)text;
        if (asRenderableText.BitmapFont != fontToSet)
        {
            asRenderableText.BitmapFont = fontToSet;

            // we want to update if the text's size is based on its "children" (the letters it contains)
            if (graphicalUiElement.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                graphicalUiElement.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                graphicalUiElement.UpdateLayout();
            }
        }
    }

    private static BitmapFont? GetFontDisposable(string fontName)
    {
#if KNI
        string prefix = "KniGum";
#elif FNA
        string prefix = "FnaGum";
#else
        string prefix = "MonoGameGum.Content";
#endif
        
        string fontFilenameOnly = Path.GetFileName(fontName);
        string embeddedFontName = $"EmbeddedResource.{prefix}.{fontFilenameOnly}";
        return global::RenderingLibrary.Content.LoaderManager.Self.GetDisposable(embeddedFontName) as BitmapFont;
    }

#endregion

    private static bool TrySetPropertyOnLineRectangle(IRenderableIpso mContainedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;

        if (propertyName == "Alpha")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineRectangle)mContainedObjectAsIpso).Color;
            color = color.WithAlpha((byte)valueAsInt);

            ((LineRectangle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Red")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineRectangle)mContainedObjectAsIpso).Color;
            color = color.WithRed((byte)valueAsInt);

            ((LineRectangle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Green")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineRectangle)mContainedObjectAsIpso).Color;
            color = color.WithGreen((byte)valueAsInt);

            ((LineRectangle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Blue")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineRectangle)mContainedObjectAsIpso).Color;
            color = color.WithBlue((byte)valueAsInt);

            ((LineRectangle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }
        else if (propertyName == "Color")
        {
            //var valueAsColor = (Color)value;
            //((LineRectangle)mContainedObjectAsIpso).Color = valueAsColor;
            var lineRectangle = (LineRectangle) mContainedObjectAsIpso;
            //var valueAsColor = (Color)value;
            if (value is System.Drawing.Color drawingColor)
            {
                lineRectangle.Color = drawingColor;
            }
            else if (value is Microsoft.Xna.Framework.Color xnaColor)
            {
                lineRectangle.Color = xnaColor.ToSystemDrawing();

            }

            handled = true;
        }
        else if(propertyName == "IsRenderTarget")
        {
            ((LineRectangle)mContainedObjectAsIpso).IsRenderTarget = value as bool? ?? false;
            handled = true;
        }

        return handled;
    }

    private static bool TrySetPropertyOnLineCircle(IRenderableIpso mContainedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;

        if (propertyName == "Alpha")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineCircle)mContainedObjectAsIpso).Color;
            color = color.WithAlpha((byte)valueAsInt);

            ((LineCircle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Red")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineCircle)mContainedObjectAsIpso).Color;
            color = color.WithRed((byte)valueAsInt);

            ((LineCircle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Green")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineCircle)mContainedObjectAsIpso).Color;
            color = color.WithGreen((byte)valueAsInt);

            ((LineCircle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Blue")
        {
            int valueAsInt = (int)value;

            var color =
                ((LineCircle)mContainedObjectAsIpso).Color;
            color = color.WithBlue((byte)valueAsInt);

            ((LineCircle)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Color")
        {
            if(value is System.Drawing.Color drawingColor)
            {
                ((LineCircle)mContainedObjectAsIpso).Color = drawingColor;
            }
            else if(value is Microsoft.Xna.Framework.Color xnaColor)
            {
                ((LineCircle)mContainedObjectAsIpso).Color = xnaColor.ToSystemDrawing();
            }
            handled = true;
        }

        else if (propertyName == "Radius")
        {
            var valueAsFloat = (float)value;
            ((LineCircle)mContainedObjectAsIpso).Width = 2 * valueAsFloat;
            ((LineCircle)mContainedObjectAsIpso).Height = 2 * valueAsFloat;
            ((LineCircle)mContainedObjectAsIpso).Radius = valueAsFloat;
            graphicalUiElement.Width = 2 * valueAsFloat;
            graphicalUiElement.Height = 2 * valueAsFloat;
        }

        return handled;
    }

    private static bool TrySetPropertyOnLinePolygon(IRenderableIpso mContainedObjectAsIpso, string propertyName, object value)
    {
        bool handled = false;


        if (propertyName == "Alpha")
        {
            int valueAsInt = (int)value;

            var color =
                ((LinePolygon)mContainedObjectAsIpso).Color;
            color = color.WithAlpha((byte)valueAsInt);

            ((LinePolygon)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Red")
        {
            int valueAsInt = (int)value;

            var color =
                ((LinePolygon)mContainedObjectAsIpso).Color;
            color = color.WithRed((byte)valueAsInt);

            ((LinePolygon)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Green")
        {
            int valueAsInt = (int)value;

            var color =
                ((LinePolygon)mContainedObjectAsIpso).Color;
            color = color.WithGreen((byte)valueAsInt);

            ((LinePolygon)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Blue")
        {
            int valueAsInt = (int)value;

            var color =
                ((LinePolygon)mContainedObjectAsIpso).Color;
            color = color.WithBlue((byte)valueAsInt);

            ((LinePolygon)mContainedObjectAsIpso).Color = color;
            handled = true;
        }

        else if (propertyName == "Color")
        {
            var valueAsColor = (Color)value;
            ((LinePolygon)mContainedObjectAsIpso).Color = valueAsColor;
            handled = true;
        }


        else if (propertyName == "Points")
        {
            var points = (List<Vector2>)value;

            ((LinePolygon)mContainedObjectAsIpso).SetPoints(points);
            handled = true;
        }

        return handled;
    }

    public static bool AssignSourceFileOnSprite(Sprite sprite, GraphicalUiElement graphicalUiElement, string value)
    {
        bool handled;

        var loaderManager =
            global::RenderingLibrary.Content.LoaderManager.Self;

        if (string.IsNullOrEmpty(value))
        {
            sprite.Texture = null;

            graphicalUiElement.UpdateLayout();
        }
        else if (value.EndsWith(".achx"))
        {
            AnimationChainList animationChainList = GetAnimationChainList(ref value, loaderManager);

            sprite.AnimationChains = animationChainList;

            sprite.RefreshCurrentChainToDesiredName();

            sprite.UpdateToCurrentAnimationFrame();

            graphicalUiElement.UpdateTextureValuesFrom(sprite);
            handled = true;
        }
        else
        {
            if (ToolsUtilities.FileManager.IsRelative(value) && ToolsUtilities.FileManager.IsUrl(value) == false)
            {
                value = ToolsUtilities.FileManager.RelativeDirectory + value;

                value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
            }

            // see if an atlas exists:
            var atlasedTexture = loaderManager.TryLoadContent<AtlasedTexture>(value);

            if (atlasedTexture != null)
            {
                graphicalUiElement.UpdateLayout();
            }
            else
            {
                // We used to check if the file exists. But internally something may
                // alias a file. Ultimately the content loader should make that decision,
                // not the GUE
                try
                {
                    sprite.Texture = loaderManager.LoadContent<Microsoft.Xna.Framework.Graphics.Texture2D>(value);
                }
                catch (Exception ex)
                // Jan 1, 2025 - we used to only catch certain types of exceptions, but this list keeps growing as there
                // are a variety of types of crashes that can occur. NineSlice catches all exceptions, so let's just do that!
                //when (ex is System.IO.FileNotFoundException or System.IO.DirectoryNotFoundException or WebException or IOException)
                {
                    if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
                    {
                        string message = $"Error setting SourceFile on Sprite";

                        if(graphicalUiElement.Tag != null)
                        {
                            message += $" in {graphicalUiElement.Tag}";
                        }
                        message += $"\n{value}";
                        message += "\nCheck if the file exists. If necessary, set FileManager.RelativeDirectory";
                        message += "\nThe current relative directory is:\n" + ToolsUtilities.FileManager.RelativeDirectory;
                        if(ObjectFinder.Self.GumProjectSave == null)
                        {
                            message += "\nNo Gum project has been loaded";
                        }

                        throw new System.IO.FileNotFoundException(message, ex);
                    }
                    sprite.Texture = null;
                }
                graphicalUiElement.UpdateLayout();
            }
        }
        handled = true;
        return handled;
    }

    private static AnimationChainList GetAnimationChainList(ref string value, 
        // fully qualify to avoid Android namign conflicts
        global::RenderingLibrary.Content.LoaderManager loaderManager)
    {
        if (ToolsUtilities.FileManager.IsRelative(value))
        {
            value = ToolsUtilities.FileManager.RelativeDirectory + value;

            value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
        }

        AnimationChainList animationChainList = null;

        if (loaderManager.CacheTextures)
        {
            animationChainList = loaderManager.GetDisposable(value) as AnimationChainList;
        }

        if (animationChainList == null)
        {
            var animationChainListSave = AnimationChainListSave.FromFile(value);
            animationChainList = animationChainListSave.ToAnimationChainList(null);
            if (loaderManager.CacheTextures)
            {
                loaderManager.AddDisposable(value, animationChainList);
            }
        }

        return animationChainList;
    }

    public static void AddRenderableToManagers(IRenderableIpso renderable, ISystemManagers iSystemManagers, Layer layer)
    {
        var managers = iSystemManagers as SystemManagers;

        if (renderable is Sprite)
        {
            managers.SpriteManager.Add(renderable as Sprite, layer);
        }
        else if (renderable is NineSlice)
        {
            managers.SpriteManager.Add(renderable as NineSlice, layer);
        }
        else if (renderable is LineRectangle)
        {
            managers.ShapeManager.Add(renderable as LineRectangle, layer);
        }
        else if (renderable is SolidRectangle)
        {
            managers.ShapeManager.Add(renderable as SolidRectangle, layer);
        }
        else if (renderable is Text)
        {
            managers.TextManager.Add(renderable as Text, layer);
        }
        else if (renderable is LineCircle)
        {
            managers.ShapeManager.Add(renderable as LineCircle, layer);
        }
        else if (renderable is LinePolygon)
        {
            managers.ShapeManager.Add(renderable as LinePolygon, layer);
        }
        else if (renderable is InvisibleRenderable)
        {
            managers.SpriteManager.Add(renderable as InvisibleRenderable, layer);
        }
        else
        {
            if (layer == null)
            {
                managers.Renderer.Layers[0].Add(renderable);
            }
            else
            {
                layer.Add(renderable);
            }
        }
    }

    public static void RemoveRenderableFromManagers(IRenderableIpso renderable, ISystemManagers iSystemManagers)
    {
        var managers = iSystemManagers as SystemManagers;

        if (renderable is Sprite)
        {
            managers.SpriteManager.Remove(renderable as Sprite);
        }
        else if (renderable is NineSlice)
        {
            managers.SpriteManager.Remove(renderable as NineSlice);
        }
        else if (renderable is global::RenderingLibrary.Math.Geometry.LineRectangle)
        {
            managers.ShapeManager.Remove(renderable as global::RenderingLibrary.Math.Geometry.LineRectangle);
        }
        else if (renderable is global::RenderingLibrary.Math.Geometry.LinePolygon)
        {
            managers.ShapeManager.Remove(renderable as global::RenderingLibrary.Math.Geometry.LinePolygon);
        }
        else if (renderable is global::RenderingLibrary.Graphics.SolidRectangle)
        {
            managers.ShapeManager.Remove(renderable as global::RenderingLibrary.Graphics.SolidRectangle);
        }
        else if (renderable is Text)
        {
            managers.TextManager.Remove(renderable as Text);
        }
        else if (renderable is LineCircle)
        {
            managers.ShapeManager.Remove(renderable as LineCircle);
        }
        else if (renderable is InvisibleRenderable)
        {
            managers.SpriteManager.Remove(renderable as InvisibleRenderable);
        }
        else if (renderable != null)
        {
            // This could be a custom visual object, so don't do anything:
            //throw new NotImplementedException();
            managers.Renderer.RemoveRenderable(renderable);
        }
        if (renderable is IManagedObject asManagedObject)
        {
            asManagedObject.RemoveFromManagers();
        }
    }

    public static void ThrowExceptionsForMissingFiles(GraphicalUiElement graphicalUiElement)
    {
#if MONOGAME || KNI
        // We can't throw exceptions when assigning values on fonts because the font values get set one-by-one
        // and the end result of all values determines which file to load. For example, an object may set the following
        // variables one-by-one:
        // * FontSize
        // * Font
        // * OutlineThickness
        // Let's say the Font gets set to Arial. The FontSize may not have been set yet, so whatever value happens
        // to be there will be used to load the font (like 12). But the user may not have Arial12 in their project,
        // and if we threw an exception on-the-spot, the user would see a message about missing Arial12, even though
        // the project doesn't actually use Arial12.
        // We need to wait until the graphical UI element is fully created before we try to throw an exception, so
        // that's what we're going to do here:
        if (graphicalUiElement != null && graphicalUiElement.RenderableComponent is Text asText)
        {

#if FRB
        // FRB doesn't yet have a TextRuntime, so we have to do this:
        var textRuntime = graphicalUiElement;
#else
            var textRuntime = graphicalUiElement as MonoGameGum.GueDeriving.TextRuntime;

#endif


            // check it
            if (asText.BitmapFont == null)
            {
                if (textRuntime.UseCustomFont)
                {
                    var fontName = ToolsUtilities.FileManager.Standardize(textRuntime.CustomFontFile, preserveCase: true, makeAbsolute: true);

                    throw new System.IO.FileNotFoundException($"Missing:{fontName}");
                }
                else
                {
                    if (textRuntime.FontSize > 0 && !string.IsNullOrEmpty(textRuntime.Font))
                    {
                        string fontName = global::RenderingLibrary.Graphics.Fonts.BmfcSave.GetFontCacheFileNameFor(
                            textRuntime.FontSize,
                            textRuntime.Font,
                            textRuntime.OutlineThickness,
                            textRuntime.UseFontSmoothing,
                            textRuntime.IsItalic,
                            textRuntime.IsBold);

                        var standardized = ToolsUtilities.FileManager.Standardize(fontName, preserveCase: true, makeAbsolute: true);

                        throw new System.IO.FileNotFoundException($"Missing:{standardized}");
                    }
                }
            }
            else
            {
                // we have a valid font file, so let's make sure the BitmapFont matches the expected font
                if (textRuntime.UseCustomFont)
                {
                    var expectedFont = textRuntime.CustomFontFile?.Replace("\\", "/");
                    var currentFont = asText.BitmapFont.FontFile?.Replace("\\", "/");

                    if (expectedFont != null && !expectedFont.Equals(currentFont, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new System.IO.FileNotFoundException($"Expected:{expectedFont} but currently using:{currentFont}");
                    }
                }
            }
        }
#endif

        foreach (var element in graphicalUiElement.ContainedElements)
        {
            ThrowExceptionsForMissingFiles(element);
        }
    }
}
