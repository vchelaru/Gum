using Gum.DataTypes;
using Gum.Managers;
using Gum.Renderables;
using Gum.Wireframe;
using MonoGameGum.Localization;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.Renderables;
internal static class CustomSetPropertyOnRenderable
{
    public static ILocalizationService LocalizationService { get; set; }

    public static void SetPropertyOnRenderable(IRenderableIpso renderableIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;

        // First try special-casing.  

        if (renderableIpso is Text asText)
        {
            handled = TrySetPropertyOnText(asText, graphicalUiElement, propertyName, value);
        }
        else if (renderableIpso is Sprite)
        {
            handled = TrySetPropertyOnSprite(renderableIpso, graphicalUiElement, propertyName, value);
        }
        else if (renderableIpso is NineSlice)
        {
            handled = TrySetPropertyOnNineSlice(renderableIpso, graphicalUiElement, propertyName, value, handled);
        }

        if (!handled)
        {
            GraphicalUiElement.SetPropertyThroughReflection(renderableIpso, graphicalUiElement, propertyName, value);
            //SetPropertyOnRenderable(mContainedObjectAsIpso, propertyName, value);
        }
    }


    private static bool TrySetPropertyOnNineSlice(IRenderableIpso renderableIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value, bool handled)
    {
        var nineSlice = renderableIpso as NineSlice;

        if (propertyName == "SourceFile")
        {
            AssignSourceFileOnNineSlice(value as string, graphicalUiElement, nineSlice);
            handled = true;
        }
        //else if (propertyName == "Blend")
        //{
        //    var valueAsGumBlend = (RenderingLibrary.Blend)value;

        //    var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

        //    nineSlice.BlendState = valueAsXnaBlend;

        //    handled = true;
        //}
        //else if (propertyName == nameof(NineSlice.CustomFrameTextureCoordinateWidth))
        //{
        //    var asFloat = value as float?;

        //    nineSlice.CustomFrameTextureCoordinateWidth = asFloat;

        //    handled = true;
        //}
        else if (propertyName == "Color")
        {
            // todo - need to convert
            //if (value is System.Drawing.Color drawingColor)
            //{
            //    nineSlice.Color = drawingColor;
            //}
            //else if (value is Microsoft.Xna.Framework.Color xnaColor)
            //{
            //    nineSlice.Color = xnaColor.ToSystemDrawing();

            //}
            //handled = true;
        }
        else if (propertyName == "Red")
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
        // not yet supported:
        //else if (value.EndsWith(".achx"))
        //{
        //    AnimationChainList animationChainList = GetAnimationChainList(ref value, loaderManager);

        //    nineSlice.AnimationChains = animationChainList;

        //    nineSlice.RefreshCurrentChainToDesiredName();

        //    nineSlice.UpdateToCurrentAnimationFrame();

        //    graphicalUiElement.UpdateTextureValuesFrom(nineSlice);

        //}
        else
        {
            if (ToolsUtilities.FileManager.IsRelative(value))
            {
                value = ToolsUtilities.FileManager.RelativeDirectory + value;
                value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
            }

            //check if part of atlas
            //Note: assumes that if this filename is in an atlas that all 9 are in an atlas
            //var atlasedTexture = global::RenderingLibrary.Content.LoaderManager.Self.TryLoadContent<AtlasedTexture>(value);
            //if (atlasedTexture != null)
            //{
            //    nineSlice.LoadAtlasedTexture(value, atlasedTexture);
            //}
            //else
            {
                //if (NineSliceExtensions.GetIfShouldUsePattern(value))
                //{
                //    nineSlice.SetTexturesUsingPattern(value, SystemManagers.Default, false);
                //}
                //else
                {

                    //Texture2D? texture = Sprite.InvalidTexture;
                    Texture2D? texture = null;

                    try
                    {
                        texture =
                            loaderManager.LoadContent<Texture2D>(value);
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
        //else if (propertyName == nameof(Sprite.Color))
        //{
        //    if (value is System.Drawing.Color drawingColor)
        //    {
        //        sprite.Color = drawingColor;
        //    }
        //    else if (value is Microsoft.Xna.Framework.Color xnaColor)
        //    {
        //        sprite.Color = xnaColor.ToSystemDrawing();

        //    }
        //    handled = true;
        //}

        //else if (propertyName == "Blend")
        //{
        //    var valueAsGumBlend = (RenderingLibrary.Blend)value;

        //    var valueAsXnaBlend = valueAsGumBlend.ToBlendState();

        //    sprite.BlendState = valueAsXnaBlend;

        //    handled = true;
        //}
        //else if (propertyName == nameof(Sprite.Animate))
        //{
        //    sprite.Animate = (bool)value;
        //    handled = true;
        //}
        //else if (propertyName == nameof(Sprite.CurrentChainName))
        //{
        //    sprite.CurrentChainName = (string)value;
        //    graphicalUiElement.UpdateTextureValuesFrom(sprite);
        //    graphicalUiElement.UpdateLayout();
        //    handled = true;
        //}

        return handled;
    }

    private static bool TrySetPropertyOnText(Text asText, GraphicalUiElement gue, string propertyName, object value)
    {
        bool handled = false;
        if (propertyName == "Text")
        {
            if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                gue.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                // make it have no line wrap width before assignign the text:
                asText.Width = 0;
            }

            asText.RawText = value as string;
            // we want to update if the text's size is based on its "children" (the letters it contains)
            if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                gue.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                gue.UpdateLayout();
            }
            handled = true;
        }
        else if(propertyName == "Font")
        {
            if(value is Font font)
            {
                asText.Font = font;
                handled = true;
            }
            else if(value is string fontString)
            {
                var fontFromGum = global::RenderingLibrary.Content.LoaderManager.Self.LoadContent<Font>(fontString);
                asText.Font = fontFromGum;
                handled = true;
            }
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
        //else if (value.EndsWith(".achx"))
        //{
        //    AnimationChainList animationChainList = GetAnimationChainList(ref value, loaderManager);

        //    sprite.AnimationChains = animationChainList;

        //    sprite.RefreshCurrentChainToDesiredName();

        //    sprite.UpdateToCurrentAnimationFrame();

        //    graphicalUiElement.UpdateTextureValuesFrom(sprite);
        //    handled = true;
        //}
        else
        {
            if (ToolsUtilities.FileManager.IsRelative(value) && ToolsUtilities.FileManager.IsUrl(value) == false)
            {
                value = ToolsUtilities.FileManager.RelativeDirectory + value;

                value = ToolsUtilities.FileManager.RemoveDotDotSlash(value);
            }

            // see if an atlas exists:
            //var atlasedTexture = loaderManager.TryLoadContent<AtlasedTexture>(value);

            //if (atlasedTexture != null)
            //{
            //    graphicalUiElement.UpdateLayout();
            //}
            //else
            {
                // We used to check if the file exists. But internally something may
                // alias a file. Ultimately the content loader should make that decision,
                // not the GUE
                try
                {
                    sprite.Texture = loaderManager.LoadContent<Texture2D>(value);
                }
                catch (Exception ex)
                // Jan 1, 2025 - we used to only catch certain types of exceptions, but this list keeps growing as there
                // are a variety of types of crashes that can occur. NineSlice catches all exceptions, so let's just do that!
                //when (ex is System.IO.FileNotFoundException or System.IO.DirectoryNotFoundException or WebException or IOException)
                {
                    if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
                    {
                        string message = $"Error setting SourceFile on Sprite";

                        if (graphicalUiElement.Tag != null)
                        {
                            message += $" in {graphicalUiElement.Tag}";
                        }
                        message += $"\n{value}";
                        message += "\nCheck if the file exists. If necessary, set FileManager.RelativeDirectory";
                        message += "\nThe current relative directory is:\n" + ToolsUtilities.FileManager.RelativeDirectory;
                        if (ObjectFinder.Self.GumProjectSave == null)
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



    public static void AddRenderableToManagers(IRenderableIpso renderable, ISystemManagers iSystemManagers, Layer layer)
    {
        var managers = iSystemManagers as SystemManagers;

        //if (renderable is Sprite)
        //{
        //    managers.SpriteManager.Add(renderable as Sprite, layer);
        //}
        //else if (renderable is NineSlice)
        //{
        //    managers.SpriteManager.Add(renderable as NineSlice, layer);
        //}
        //else if (renderable is LineRectangle)
        //{
        //    managers.ShapeManager.Add(renderable as LineRectangle, layer);
        //}
        //else if (renderable is SolidRectangle)
        //{
        //    managers.ShapeManager.Add(renderable as SolidRectangle, layer);
        //}
        //else if (renderable is Text)
        //{
        //    managers.TextManager.Add(renderable as Text, layer);
        //}
        //else if (renderable is LineCircle)
        //{
        //    managers.ShapeManager.Add(renderable as LineCircle, layer);
        //}
        //else if (renderable is LinePolygon)
        //{
        //    managers.ShapeManager.Add(renderable as LinePolygon, layer);
        //}
        //else if (renderable is InvisibleRenderable)
        //{
        //    managers.SpriteManager.Add(renderable as InvisibleRenderable, layer);
        //}
        //else
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
}
