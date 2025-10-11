using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.RenderingLibrary;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#if SKIA
using HarfBuzzSharp;
using SkiaGum.Content;
using SkiaGum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
using RenderableShapeBase = SkiaGum.Renderables.RenderableShapeBase;
namespace SkiaGum;
#else
using MonoGameAndGum.Renderables;
using MonoGameGum.GueDeriving;
namespace MonoGameGumShapes;

#endif


public class CustomSetPropertyOnRenderable
{

    private static bool TrySetPropertiesOnRenderableBase(RenderableShapeBase renderableBase, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch (propertyName)
        {
            case nameof(RenderableShapeBase.Alpha):
                renderableBase.Alpha = (int)value;
                return true;
            case nameof(RenderableShapeBase.Alpha1):
                renderableBase.Alpha1 = (int)value;
                return true;
            case nameof(RenderableShapeBase.Alpha2):
                renderableBase.Alpha2 = (int)value;
                return true;
            case nameof(RenderableShapeBase.Blue):
                renderableBase.Blue = (int)value;
                return true;
            case nameof(RenderableShapeBase.Blue1):
                renderableBase.Blue1 = (int)value;
                return true;
            case nameof(RenderableShapeBase.Blue2):
                renderableBase.Blue2 = (int)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowAlpha):
                renderableBase.DropshadowAlpha = (int)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowBlue):
                renderableBase.DropshadowBlue = (int)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowBlurX):
                renderableBase.DropshadowBlurX = (float)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowBlurY):
                renderableBase.DropshadowBlurY = (float)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowGreen):
                renderableBase.DropshadowGreen = (int)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowOffsetX):
                renderableBase.DropshadowOffsetX = (float)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowOffsetY):
                renderableBase.DropshadowOffsetY = (float)value;
                return true;
            case nameof(RenderableShapeBase.DropshadowRed):
                renderableBase.DropshadowRed = (int)value;
                return true;
            case nameof(RenderableShapeBase.GradientInnerRadius):
                renderableBase.GradientInnerRadius = (float)value;
                return true;
            case nameof(RenderableShapeBase.GradientInnerRadiusUnits):
                renderableBase.GradientInnerRadiusUnits = (DimensionUnitType)value;
                return true;
            case nameof(RenderableShapeBase.GradientOuterRadius):
                renderableBase.GradientOuterRadius = (float)value;
                return true;
            case nameof(RenderableShapeBase.GradientOuterRadiusUnits):
                renderableBase.GradientOuterRadiusUnits = (DimensionUnitType)value;
                return true;
            case nameof(RenderableShapeBase.GradientType):
                renderableBase.GradientType = (GradientType)value;
                return true;
            case nameof(RenderableShapeBase.GradientX1):
                renderableBase.GradientX1 = (float)value;
                return true;
            case nameof(RenderableShapeBase.GradientX1Units):
                {
                    if (value is PositionUnitType positionUnitType)
                    {
                        renderableBase.GradientX1Units = UnitConverter.ConvertToGeneralUnit(positionUnitType);
                    }
                    else
                    {
                        renderableBase.GradientX1Units = (GeneralUnitType)value;
                    }
                }
                return true;
            case nameof(RenderableShapeBase.GradientX2):
                renderableBase.GradientX2 = (float)value;
                return true;
            case nameof(RenderableShapeBase.GradientX2Units):
                {
                    if (value is PositionUnitType positionUnitType)
                    {
                        renderableBase.GradientX2Units = UnitConverter.ConvertToGeneralUnit(positionUnitType);
                    }
                    else
                    {
                        renderableBase.GradientX2Units = (GeneralUnitType)value;
                    }
                }
                return true;
            case nameof(RenderableShapeBase.GradientY1):
                renderableBase.GradientY1 = (float)value;
                return true;
            case nameof(RenderableShapeBase.GradientY1Units):
                {
                    if(value is PositionUnitType positionUnitType)
                    {
                        renderableBase.GradientY1Units = UnitConverter.ConvertToGeneralUnit(positionUnitType);
                    }
                    else
                    {
                        renderableBase.GradientY1Units = (GeneralUnitType)value;
                    }
                }
                return true;
            case nameof(RenderableShapeBase.GradientY2):
                renderableBase.GradientY2 = (float)value;
                return true;
            case nameof(RenderableShapeBase.GradientY2Units):
                {
                    if(value is PositionUnitType positionUnitType)
                    {
                        renderableBase.GradientY2Units = UnitConverter.ConvertToGeneralUnit(positionUnitType);
                    }
                    else
                    {
                        renderableBase.GradientY2Units = (GeneralUnitType)value;
                    }
                }

                return true;
            case nameof(RenderableShapeBase.Green):
                renderableBase.Green = (int)value;
                return true;
            case nameof(RenderableShapeBase.Green1):
                renderableBase.Green1 = (int)value;
                return true;
            case nameof(RenderableShapeBase.Green2):
                renderableBase.Green2 = (int)value;
                return true;
            case nameof(RenderableShapeBase.HasDropshadow):
                renderableBase.HasDropshadow = (bool)value;
                return true;
            case nameof(RenderableShapeBase.IsFilled):
                renderableBase.IsFilled = (bool)value;
                return true;
            case nameof(RenderableShapeBase.Red):
                renderableBase.Red = (int)value;
                return true;
            case nameof(RenderableShapeBase.Red1):
                renderableBase.Red1 = (int)value;
                return true;
            case nameof(RenderableShapeBase.Red2):
                renderableBase.Red2 = (int)value;
                return true;
            case nameof(RenderableShapeBase.StrokeWidth):
                renderableBase.StrokeWidth = (float)value;
                return true;
            case nameof(RenderableShapeBase.UseGradient):
                renderableBase.UseGradient = (bool)value;
                return true;
                // todo - more here...
        }
        return false;
    }

    private static bool TrySetPropertyOnArc(Arc asArc, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch(propertyName)
        {
            case nameof(Arc.Thickness):
                asArc.Thickness = (float)value;
                return true;
        }
        return false;
    }

    private static bool TrySetPropertyOnRoundedRectangle(RoundedRectangle asRoundedRectangle, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch (propertyName) 
        {
            case nameof(RoundedRectangle.CornerRadius):
                asRoundedRectangle.CornerRadius = (float)value;
                return true;
        }

        return false;
    }

    // todo - fill this out for the sake of performance...
    // If it's not filled out, then properties will be set by reflection
    // It would be nice to share this with MonoGame at some point too so 
    // we don't have to keep everything in duplicate
    public static void SetPropertyOnRenderable(IRenderableIpso containedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value) =>
        SetPropertyOnRenderableFunc(containedObjectAsIpso, graphicalUiElement, propertyName, value);

    public static bool SetPropertyOnRenderableFunc(IRenderableIpso containedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;
        if(containedObjectAsIpso is Arc asArc)
        {
            handled = TrySetPropertiesOnRenderableBase(asArc, graphicalUiElement, propertyName, value);
            if(!handled)
            {
                handled = TrySetPropertyOnArc(asArc, graphicalUiElement, propertyName, value);
            }
        }
        else if(containedObjectAsIpso is RoundedRectangle asRoundedRectangle)
        {
            // some properties have priority on the base shape itself:
            switch (propertyName)
            {
                case nameof(RoundedRectangleRuntime.StrokeWidth):
                    ((RoundedRectangleRuntime)graphicalUiElement).StrokeWidth = (float)value;
                    handled = true;
                    break;
            }
            if (!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asRoundedRectangle, graphicalUiElement, propertyName, value);
            }
            if(!handled)
            {
                handled = TrySetPropertyOnRoundedRectangle(asRoundedRectangle, graphicalUiElement, propertyName, value);
            }
        }
        else if (containedObjectAsIpso is Circle asCircle)
        {
            if(graphicalUiElement is CircleRuntime circleRuntime)
            {
                switch (propertyName)
                {
                    case "Radius":
                        var radius = (float)value;
                        graphicalUiElement.Width = radius * 2;
                        graphicalUiElement.Height = radius * 2;
                        handled = true;
                        break;
                }
            }

            switch(propertyName)
            {
                case nameof(ColoredCircleRuntime.StrokeWidth):
                    ((ColoredCircleRuntime)graphicalUiElement).StrokeWidth = (float)value;
                    handled = true;
                    break;
            }

            if(!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asCircle, graphicalUiElement, propertyName, value);
            }
            if (!handled)
            {
                // todo - if there end up being circle-specific properties, set them here
                //handled = TrySetPropertyOnRoundedRectangle(asRoundedRectangle, graphicalUiElement, propertyName, value);
            }
        }
#if SKIA
        else if (containedObjectAsIpso is Text asText)
        {
            handled = TrySetPropertyOnText(asText, graphicalUiElement, propertyName, value);
        }
        else if(containedObjectAsIpso is VectorSprite asSvg)
        {
            //handled = TrySetPropertiesOnRenderableBase(asSvg, graphicalUiElement, propertyName, value);
            if(!handled)
            {
                handled = TrySetPropertyOnSvg(asSvg, graphicalUiElement, propertyName, value);
            }
        }
        else if(containedObjectAsIpso is Sprite asSprite)
        {
            if(!handled)
            {
                handled = TrySetPropertyOnSprite(asSprite, graphicalUiElement, propertyName, value);
            }
        }
        else if(containedObjectAsIpso is Polygon asPolygon)
        {
            if(!handled)
            {
                handled = TrySetPropertyOnPolygon(asPolygon, graphicalUiElement, propertyName, value);
            }
        }
        if (!handled)
        {
            GraphicalUiElement.SetPropertyThroughReflection(containedObjectAsIpso, graphicalUiElement, propertyName, value);
            //SetPropertyOnRenderable(mContainedObjectAsIpso, propertyName, value);
        }
#endif

        return handled;
    }

#if SKIA

    private static bool TrySetPropertyOnPolygon(Polygon asPolygon, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch(propertyName)
        {
            case "Points":
                if(value is List<System.Numerics.Vector2> vector2s)
                {
                    var toAssign = new List<SKPoint>();
                    toAssign.AddRange(vector2s.Select(item => new SKPoint(item.X, item.Y)));
                    asPolygon.Points = toAssign;
                    return true;
                }
                //asPolygon.Points = ;
                break;
        }
        return false;
    }

    private static bool TrySetPropertyOnSprite(Sprite asSprite, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch(propertyName)
        {
            case "SourceFile":
                var asString = value as string;
                if(graphicalUiElement is SpriteRuntime spriteRuntime)
                {
                    spriteRuntime.SourceFile = asString;
                }
                else if (!string.IsNullOrEmpty(asString))
                {
                    var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                    var contentLoader = loaderManager.ContentLoader;
                    var image = contentLoader.LoadContent<SKBitmap>(asString);
                    asSprite.Texture = image;
                }
                else
                {
                    asSprite.Texture = null;
                }
                break;
        }
        return false;
    }

    private static bool TrySetPropertyOnSvg(VectorSprite asSvg, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch(propertyName) 
        {
            case "SourceFile":
                var asString = value as string;

                if(!string.IsNullOrEmpty(asString))
                {
                    asSvg.Texture = SkiaResourceManager.GetSvg(asString);
                }
                else
                {
                    asSvg.Texture = null;
                }
                break;
        }
        return false;
    }


    public static void UpdateToFontValues(IText itext, GraphicalUiElement graphicalUiElement)
    {
        // BitmapFont font = null;

        var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
        var contentLoader = loaderManager.ContentLoader;

        //if(UseCustomFont)
        //{

        //}
        //else
        {
            if(graphicalUiElement is TextRuntime asTextRuntime && !string.IsNullOrEmpty(asTextRuntime.Font))
            {
                //SKTypeface font = contentLoader.LoadContent<SKTypeface>(Font);
                if (asTextRuntime.Font != null && itext is Text text)
                {
                    text.FontName = asTextRuntime.Font;
                    text.FontSize = asTextRuntime.FontSize;
                }
            }
        }
    }



    private static bool TrySetPropertyOnText(Text text, GraphicalUiElement gue, string propertyName, object value)
    {
        bool handled = false;

        var gueAsTextRuntime = gue as TextRuntime;

        void ReactToFontValueChange()
        {
            gue.UpdateToFontValues();
            // we want to update if the text's size is based on its "children" (the letters it contains)
            if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                gue.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                gue.UpdateLayout();
            }
            handled = true;
        }

        if (propertyName == "Text")
        {
            if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                gue.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                // make it have no line wrap width before assignign the text:
                text.Width = 0;
            }

            text.RawText = value as string;
            // we want to update if the text's size is based on its "children" (the letters it contains)
            if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                gue.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                gue.UpdateLayout();
            }
            handled = true;
        }
        else if (propertyName == "Font Scale" || propertyName == "FontScale")
        {
            text.FontScale = (float)value;
            // we want to update if the text's size is based on its "children" (the letters it contains)
            if (gue.WidthUnits == DimensionUnitType.RelativeToChildren ||
                // If height is relative to children, it could be in a stack
                gue.HeightUnits == DimensionUnitType.RelativeToChildren)
            {
                gue.UpdateLayout();
            }
            handled = true;

        }
        else if (propertyName == "Font")
        {
            if(gueAsTextRuntime != null)
            {
                gueAsTextRuntime.Font = value as string;
            }

            ReactToFontValueChange();
        }


        else if (propertyName == nameof(gueAsTextRuntime.FontSize))
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.FontSize = (int)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(gueAsTextRuntime.OutlineThickness))
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.OutlineThickness = (int)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(gueAsTextRuntime.IsItalic))
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.IsItalic = (bool)value;
            }
            ReactToFontValueChange();
        }
        else if (propertyName == nameof(gueAsTextRuntime.IsBold))
        {
            if (gueAsTextRuntime != null)
            {
                gueAsTextRuntime.IsBold = (bool)value;
            }
            ReactToFontValueChange();
        }
#if !SKIA
        else if (propertyName == nameof(gueAsTextRuntime.UseFontSmoothing))
        {
            if (gueAsTextRuntime != null)
            {
                gue.UseFontSmoothing = (bool)value;
            }
            ReactToFontValueChange();
        }
#endif
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
            text.Red = valueAsInt;
            handled = true;
        }
        else if (propertyName == "Green")
        {
            int valueAsInt = (int)value;
            text.Green = valueAsInt;
            handled = true;
        }
        else if (propertyName == "Blue")
        {
            int valueAsInt = (int)value;
            text.Blue = valueAsInt;
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
            text.HorizontalAlignment = (RenderingLibrary.Graphics.HorizontalAlignment)value;
            handled = true;
        }
        else if (propertyName == "VerticalAlignment")
        {
            text.VerticalAlignment = (VerticalAlignment)value;
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
                text.IsTruncatingWithEllipsisOnLastLine = true;
            }
            else
            {
                text.IsTruncatingWithEllipsisOnLastLine = false;
            }
        }
        else if (propertyName == nameof(TextOverflowVerticalMode))
        {
            var textOverflowMode = (TextOverflowVerticalMode)value;
#if MONOGAME || XNA4

            ((Text)mContainedObjectAsIpso).TextOverflowVerticalMode = textOverflowMode;
#endif

        }

        return handled;
    }

#endif

}
