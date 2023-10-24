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
using System.Runtime.CompilerServices;
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
                else if(propertyName == "Animate")
                {
                    sprite.Animate = (bool)value;
                    handled = true;
                }
                else if(propertyName == "CurrentChainName")
                {
                    sprite.CurrentChainName = (string)value;
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
                UpdateToFontValues(mContainedObjectAsIpso as IText, graphicalUiElement);
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
                graphicalUiElement.RefreshTextOverflowVerticalMode();

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

        public static void UpdateToFontValues(IText text, GraphicalUiElement graphicalUiElement)
        {
            if (graphicalUiElement.IsLayoutSuspended || GraphicalUiElement.IsAllLayoutSuspended)
            {
                graphicalUiElement.IsFontDirty = true;
            }
            // todo: This could make things faster, but it will require
            // extra calls in generated code, or an "UpdateAll" method
            //if (!mIsLayoutSuspended && !IsAllLayoutSuspended)
            else
            {
                BitmapFont font = null;

                var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                var contentLoader = loaderManager.ContentLoader;

                if (graphicalUiElement.UseCustomFont)
                {

                    if (!string.IsNullOrEmpty(graphicalUiElement.CustomFontFile))
                    {
                        font = contentLoader.TryGetCachedDisposable<BitmapFont>(graphicalUiElement.CustomFontFile);
                        if (font == null)
                        {
                            // so normally we would just let the content loader check if the file exists but since we're not going to
                            // use the content loader for BitmapFont, we're going to protect this with a file.exists.
                            if (ToolsUtilities.FileManager.FileExists(graphicalUiElement.CustomFontFile))
                            {
                                font = new BitmapFont(graphicalUiElement.CustomFontFile, SystemManagers.Default);
                                contentLoader.AddDisposable(graphicalUiElement.CustomFontFile, font);
                            }
                        }
                    }


                }
                else
                {
                    if (graphicalUiElement.FontSize > 0 && !string.IsNullOrEmpty(graphicalUiElement.Font))
                    {

                        string fontName = global::RenderingLibrary.Graphics.Fonts.BmfcSave.GetFontCacheFileNameFor(
                            graphicalUiElement.FontSize,
                            graphicalUiElement.Font,
                            graphicalUiElement.OutlineThickness,
                            graphicalUiElement.UseFontSmoothing,
                            graphicalUiElement.IsItalic,
                            graphicalUiElement.IsBold);

                        string fullFileName = ToolsUtilities.FileManager.Standardize(fontName, false, true);

#if ANDROID || IOS
                        fullFileName = fullFileName.ToLowerInvariant();
#endif


                        font = contentLoader.TryGetCachedDisposable<BitmapFont>(fullFileName);
                        if (font == null)
                        {
                            // so normally we would just let the content loader check if the file exists but since we're not going to
                            // use the content loader for BitmapFont, we're going to protect this with a file.exists.
                            if (ToolsUtilities.FileManager.FileExists(fullFileName))
                            {
                                font = new BitmapFont(fullFileName, SystemManagers.Default);

                                contentLoader.AddDisposable(fullFileName, font);
                            }
                        }

#if DEBUG
                        if (font?.Textures.Any(item => item?.IsDisposed == true) == true)
                        {
                            throw new InvalidOperationException("The returned font has a disposed texture");
                        }
#endif
                    }
                }

                ((Text)text).BitmapFont = font ?? global::RenderingLibrary.Content.LoaderManager.Self.DefaultBitmapFont;
            }
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
#if MONOGAME
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
            if (graphicalUiElement != null && graphicalUiElement.RenderableComponent is Text)
            {
                // check it
                var asText = graphicalUiElement.RenderableComponent as Text;
                if (asText.BitmapFont == null)
                {
                    if (graphicalUiElement.UseCustomFont)
                    {
                        var fontName = ToolsUtilities.FileManager.Standardize(graphicalUiElement.CustomFontFile, preserveCase: true, makeAbsolute: true);

                        throw new System.IO.FileNotFoundException($"Missing:{fontName}");
                    }
                    else
                    {
                        if (graphicalUiElement.FontSize > 0 && !string.IsNullOrEmpty(graphicalUiElement.Font))
                        {
                            string fontName = global::RenderingLibrary.Graphics.Fonts.BmfcSave.GetFontCacheFileNameFor(
                                graphicalUiElement.FontSize,
                                graphicalUiElement.Font,
                                graphicalUiElement.OutlineThickness,
                                graphicalUiElement.UseFontSmoothing,
                                graphicalUiElement.IsItalic,
                                graphicalUiElement.IsBold);

                            var standardized = ToolsUtilities.FileManager.Standardize(fontName, preserveCase: true, makeAbsolute: true);

                            throw new System.IO.FileNotFoundException($"Missing:{standardized}");
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
}
