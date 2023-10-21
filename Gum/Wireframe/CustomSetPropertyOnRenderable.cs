using Gum.DataTypes;
using Gum.Graphics.Animation;
using Gum.RenderingLibrary;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using ToolsUtilitiesStandard.Helpers;

namespace Gum.Wireframe
{
    public class CustomSetPropertyOnRenderable
    {
        public static void SetPropertyOnRenderable(IRenderableIpso mContainedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
        {
            bool handled = false;

            // First try special-casing.  

            if (mContainedObjectAsIpso is Text)
            {
                handled = TrySetPropertyOnText(mContainedObjectAsIpso, graphicalUiElement, propertyName, value);
            }
#if MONOGAME || XNA4
            else if (mContainedObjectAsIpso is LineCircle)
            {
                handled = TrySetPropertyOnLineCircle(mContainedObjectAsIpso, graphicalUiElement, propertyName, value);
            }
            else if (mContainedObjectAsIpso is LineRectangle)
            {
                handled = TrySetPropertyOnLineRectangle(mContainedObjectAsIpso, graphicalUiElement, propertyName, value);
            }
            else if (mContainedObjectAsIpso is LinePolygon)
            {
                handled = TrySetPropertyOnLinePolygon(mContainedObjectAsIpso, propertyName, value);
            }
            else if (mContainedObjectAsIpso is SolidRectangle)
            {
                var solidRect = mContainedObjectAsIpso as SolidRectangle;

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
                    var valueAsColor = (Color)value;
                    solidRect.Color = valueAsColor;
                    handled = true;
                }

            }
            else if (mContainedObjectAsIpso is Sprite)
            {
                var sprite = mContainedObjectAsIpso as Sprite;

                if (propertyName == "SourceFile")
                {
                    var asString = value as String;
                    handled = AssignSourceFileOnSprite(sprite, graphicalUiElement, asString);

                }
                else if (propertyName == "Alpha")
                {
                    int valueAsInt = (int)value;
                    sprite.Alpha = valueAsInt;
                    handled = true;
                }
                else if (propertyName == "Red")
                {
                    int valueAsInt = (int)value;
                    sprite.Red = valueAsInt;
                    handled = true;
                }
                else if (propertyName == "Green")
                {
                    int valueAsInt = (int)value;
                    sprite.Green = valueAsInt;
                    handled = true;
                }
                else if (propertyName == "Blue")
                {
                    int valueAsInt = (int)value;
                    sprite.Blue = valueAsInt;
                    handled = true;
                }
                else if (propertyName == "Color")
                {
                    var valueAsColor = (Color)value;
                    sprite.Color = valueAsColor;
                    handled = true;
                }

                else if (propertyName == "Blend")
                {
                    var valueAsGumBlend = (RenderingLibrary.Blend)value;

                    var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

                    sprite.BlendState = valueAsXnaBlend;

                    handled = true;
                }
                if (!handled)
                {
                    int m = 3;
                }
            }
            else if (mContainedObjectAsIpso is NineSlice)
            {
                var nineSlice = mContainedObjectAsIpso as NineSlice;

                if (propertyName == "SourceFile")
                {
                    string valueAsString = value as string;

                    if (string.IsNullOrEmpty(valueAsString))
                    {
                        nineSlice.SetSingleTexture(null);
                    }
                    else
                    {
                        if (ToolsUtilities.FileManager.IsRelative(valueAsString))
                        {
                            valueAsString = ToolsUtilities.FileManager.RelativeDirectory + valueAsString;
                            valueAsString = ToolsUtilities.FileManager.RemoveDotDotSlash(valueAsString);
                        }

                        //check if part of atlas
                        //Note: assumes that if this filename is in an atlas that all 9 are in an atlas
                        var atlasedTexture = global::RenderingLibrary.Content.LoaderManager.Self.TryLoadContent<AtlasedTexture>(valueAsString);
                        if (atlasedTexture != null)
                        {
                            nineSlice.LoadAtlasedTexture(valueAsString, atlasedTexture);
                        }
                        else
                        {
                            if (NineSliceExtensions.GetIfShouldUsePattern(valueAsString))
                            {
                                nineSlice.SetTexturesUsingPattern(valueAsString, SystemManagers.Default, false);
                            }
                            else
                            {
                                var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;

                                Microsoft.Xna.Framework.Graphics.Texture2D texture =
                                    global::RenderingLibrary.Content.LoaderManager.Self.InvalidTexture;

                                try
                                {
                                    texture =
                                        loaderManager.LoadContent<Microsoft.Xna.Framework.Graphics.Texture2D>(valueAsString);
                                }
                                catch (Exception e)
                                {
                                    if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
                                    {
                                        string message = $"Error setting SourceFile on NineSlice:\n{valueAsString}";
                                        throw new System.IO.FileNotFoundException(message);
                                    }
                                    // do nothing?
                                }
                                nineSlice.SetSingleTexture(texture);

                            }
                        }
                    }
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
                    System.Reflection.PropertyInfo propertyInfo = mContainedObjectAsIpso.GetType().GetProperty(propertyName);

                    if (propertyInfo != null && propertyInfo.CanWrite)
                    {

                        if (value.GetType() != propertyInfo.PropertyType)
                        {
                            value = System.Convert.ChangeType(value, propertyInfo.PropertyType);
                        }
                        propertyInfo.SetValue(mContainedObjectAsIpso, value, null);
                    }
                }
            }
        }

        private static bool TrySetPropertyOnText(IRenderableIpso mContainedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
        {
            bool handled = false;

            void ReactToFontValueChange()
            {
                graphicalUiElement.UpdateToFontValues();
                // we want to update if the text's size is based on its "children" (the letters it contains)
                if (graphicalUiElement.WidthUnits == DimensionUnitType.RelativeToChildren ||
                    // If height is relative to children, it could be in a stack
                    graphicalUiElement.HeightUnits == DimensionUnitType.RelativeToChildren)
                {
                    graphicalUiElement.UpdateLayout();
                }
                handled = true;
            }

            if (propertyName == "Text")
            {
                var asText = ((Text)mContainedObjectAsIpso);
                if (graphicalUiElement.WidthUnits == DimensionUnitType.RelativeToChildren ||
                    // If height is relative to children, it could be in a stack
                    graphicalUiElement.HeightUnits == DimensionUnitType.RelativeToChildren)
                {
                    // make it have no line wrap width before assignign the text:
                    asText.Width = 0;
                }

                asText.RawText = value as string;
                // we want to update if the text's size is based on its "children" (the letters it contains)
                if (graphicalUiElement.WidthUnits == DimensionUnitType.RelativeToChildren ||
                    // If height is relative to children, it could be in a stack
                    graphicalUiElement.HeightUnits == DimensionUnitType.RelativeToChildren)
                {
                    graphicalUiElement.UpdateLayout();
                }
                handled = true;
            }
            else if (propertyName == "Font Scale")
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
                graphicalUiElement.Font = value as string;

                ReactToFontValueChange();
            }
#if MONOGAME || XNA4
            else if (propertyName == nameof(graphicalUiElement.UseCustomFont))
            {
                graphicalUiElement.UseCustomFont = (bool)value;
                ReactToFontValueChange();
            }

            else if (propertyName == nameof(graphicalUiElement.CustomFontFile))
            {
                graphicalUiElement.CustomFontFile = (string)value;
                ReactToFontValueChange();

            }
#endif
            else if (propertyName == nameof(graphicalUiElement.FontSize))
            {
                graphicalUiElement.FontSize = (int)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(graphicalUiElement.OutlineThickness))
            {
                graphicalUiElement.OutlineThickness = (int)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(graphicalUiElement.IsItalic))
            {
                graphicalUiElement.IsItalic = (bool)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(graphicalUiElement.IsBold))
            {
                graphicalUiElement.IsBold = (bool)value;
                ReactToFontValueChange();
            }
            else if (propertyName == "LineHeightMultiplier")
            {
                var asText = ((Text)mContainedObjectAsIpso);
                asText.LineHeightMultiplier = (float)value;
            }
            else if (propertyName == nameof(graphicalUiElement.UseFontSmoothing))
            {
                graphicalUiElement.UseFontSmoothing = (bool)value;
                ReactToFontValueChange();
            }
            else if (propertyName == nameof(Blend))
            {
#if MONOGAME || XNA4
                var valueAsGumBlend = (RenderingLibrary.Blend)value;

                var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

                var text = mContainedObjectAsIpso as Text;
                text.BlendState = valueAsXnaBlend;
                handled = true;
#endif
            }
            else if (propertyName == "Alpha")
            {
#if MONOGAME || XNA4
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
#if MONOGAME || XNA4
                var valueAsColor = (Color)value;
                ((Text)mContainedObjectAsIpso).Color = valueAsColor;
                handled = true;
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
#if MONOGAME || XNA4
                ((Text)mContainedObjectAsIpso).MaxLettersToShow = (int?)value;
                handled = true;
#endif
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
#if MONOGAME || XNA4
                graphicalUiElement.RefreshTextOverflowVerrticalMode();
#endif

            }

            return handled;
        }

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
                var valueAsColor = (Color)value;
                ((LineRectangle)mContainedObjectAsIpso).Color = valueAsColor;
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
                var valueAsColor = (Color)value;
                ((LineCircle)mContainedObjectAsIpso).Color = valueAsColor;
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
                sprite.AtlasedTexture = null;

                graphicalUiElement.UpdateLayout();
            }
            else if (value.EndsWith(".achx"))
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
                    var animationChainListSave = Content.AnimationChain.AnimationChainListSave.FromFile(value);
                    animationChainList = animationChainListSave.ToAnimationChainList(null);
                    if (loaderManager.CacheTextures)
                    {
                        loaderManager.AddDisposable(value, animationChainList);
                    }
                }

                graphicalUiElement.AnimationChains = animationChainList;

                graphicalUiElement.RefreshCurrentChainToDesiredName();

                graphicalUiElement.UpdateToCurrentAnimationFrame();
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
                    sprite.AtlasedTexture = atlasedTexture;
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
                    catch (Exception ex) when (ex is System.IO.FileNotFoundException || ex is System.IO.DirectoryNotFoundException)
                    {
                        if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
                        {
                            string message = $"Error setting SourceFile on Sprite in {graphicalUiElement.Tag}:\n{value}";
                            throw new System.IO.FileNotFoundException(message);
                        }
                        sprite.Texture = null;
                    }
                    graphicalUiElement.UpdateLayout();
                }
            }
            handled = true;
            return handled;
        }

    }
}
