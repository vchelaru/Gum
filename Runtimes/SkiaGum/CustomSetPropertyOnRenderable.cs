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
using Gum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
using RenderableShapeBase = SkiaGum.Renderables.RenderableShapeBase;
namespace SkiaGum;
#else
using MonoGameAndGum.Renderables;
using Gum.GueDeriving;
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
            // .gumx stores Blend as the non-nullable Gum.RenderingLibrary.Blend enum, but the
            // property is Blend?. SetPropertyThroughReflection's Convert.ChangeType fallback
            // throws on enum -> Nullable<enum>, so the assignment has to land here. See the
            // SilkNetGum sample crash that prompted this branch — every Standards/*.gutx with
            // a default Blend variable hit the reflection path before this case was added.
            case nameof(RenderableShapeBase.Blend):
                renderableBase.Blend = (Blend)value;
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
            case nameof(RenderableShapeBase.StrokeDashLength):
                renderableBase.StrokeDashLength = (float)value;
                return true;
            case nameof(RenderableShapeBase.StrokeGapLength):
                renderableBase.StrokeGapLength = (float)value;
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
                if (graphicalUiElement is ArcRuntime arcRuntime)
                {
                    arcRuntime.Thickness = (float)value;
                }
                else
                {
                    asArc.Thickness = (float)value;
                }
                return true;
        }
        return false;
    }

    private static bool TrySetPropertyOnLine(Line asLine, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch (propertyName)
        {
            case nameof(Line.IsRounded):
                asLine.IsRounded = (bool)value;
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
            // Mirror RoundedRectangle/Line/ColoredCircle: stroke values must land on the runtime so
            // AposShapeRuntime.PreRender (which pushes the runtime values to the renderable each
            // frame, applying ScreenPixel zoom) does not clobber a value written straight to the
            // renderable. See issue #2629.
            switch (propertyName)
            {
                case nameof(ArcRuntime.StrokeWidth):
                    if (graphicalUiElement is ArcRuntime arcStrokeRuntime)
                    {
                        arcStrokeRuntime.StrokeWidth = (float)value;
                    }
                    else
                    {
                        asArc.StrokeWidth = (float)value;
                    }
                    handled = true;
                    break;
                case nameof(ArcRuntime.StrokeDashLength):
                    if (graphicalUiElement is ArcRuntime arcDashRuntime)
                    {
                        arcDashRuntime.StrokeDashLength = (float)value;
                    }
                    else
                    {
                        asArc.StrokeDashLength = (float)value;
                    }
                    handled = true;
                    break;
                case nameof(ArcRuntime.StrokeGapLength):
                    if (graphicalUiElement is ArcRuntime arcGapRuntime)
                    {
                        arcGapRuntime.StrokeGapLength = (float)value;
                    }
                    else
                    {
                        asArc.StrokeGapLength = (float)value;
                    }
                    handled = true;
                    break;
            }
            if (!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asArc, graphicalUiElement, propertyName, value);
            }
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
                    if(graphicalUiElement is RoundedRectangleRuntime asRoundedRectangleRuntime)
                    {
                        asRoundedRectangleRuntime.StrokeWidth = (float)value;

                    }
                    else
                    {
                        asRoundedRectangle.StrokeWidth = (float)value;
                    }
                    handled = true;
                    break;
                // Mirror StrokeWidth routing: dashed-stroke values live on the runtime so the Apos
                // ScreenPixel-scaling pass in PreRender stays consistent with StrokeWidth. Skia's
                // runtime is a passthrough setter so this path produces the same end state there.
                case nameof(RoundedRectangleRuntime.StrokeDashLength):
                    if (graphicalUiElement is RoundedRectangleRuntime rrDashRuntime)
                    {
                        rrDashRuntime.StrokeDashLength = (float)value;
                    }
                    else
                    {
                        asRoundedRectangle.StrokeDashLength = (float)value;
                    }
                    handled = true;
                    break;
                case nameof(RoundedRectangleRuntime.StrokeGapLength):
                    if (graphicalUiElement is RoundedRectangleRuntime rrGapRuntime)
                    {
                        rrGapRuntime.StrokeGapLength = (float)value;
                    }
                    else
                    {
                        asRoundedRectangle.StrokeGapLength = (float)value;
                    }
                    handled = true;
                    break;
                // Issue #2720: route CornerRadius and per-corner radii to RectangleRuntime when
                // that's the GUE. RectangleRuntime stores these on the runtime and mirrors to
                // fill+stroke slots in its setter (plus Skia re-pushes them each frame in
                // PreRender for ScreenPixel scaling). Writing them straight to the renderable
                // would be clobbered the next time the runtime touched the property — or, on
                // Skia, on the very next frame. RoundedRectangleRuntime is frozen and does not
                // need its own arm (no new functionality lands on it).
                case nameof(RectangleRuntime.CornerRadius):
                    if (graphicalUiElement is RectangleRuntime rectCornerRuntime)
                    {
                        rectCornerRuntime.CornerRadius = (float)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.CustomRadiusTopLeft):
                    if (graphicalUiElement is RectangleRuntime rectTLRuntime)
                    {
                        rectTLRuntime.CustomRadiusTopLeft = (float?)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.CustomRadiusTopRight):
                    if (graphicalUiElement is RectangleRuntime rectTRRuntime)
                    {
                        rectTRRuntime.CustomRadiusTopRight = (float?)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.CustomRadiusBottomLeft):
                    if (graphicalUiElement is RectangleRuntime rectBLRuntime)
                    {
                        rectBLRuntime.CustomRadiusBottomLeft = (float?)value;
                        handled = true;
                    }
                    break;
                case nameof(RectangleRuntime.CustomRadiusBottomRight):
                    if (graphicalUiElement is RectangleRuntime rectBRRuntime)
                    {
                        rectBRRuntime.CustomRadiusBottomRight = (float?)value;
                        handled = true;
                    }
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
        else if (containedObjectAsIpso is Line asLine)
        {
            switch (propertyName)
            {
                case nameof(LineRuntime.StrokeWidth):
                    if (graphicalUiElement is LineRuntime asLineRuntime)
                    {
                        asLineRuntime.StrokeWidth = (float)value;
                    }
                    else
                    {
                        asLine.StrokeWidth = (float)value;
                    }
                    handled = true;
                    break;
                case nameof(LineRuntime.StrokeDashLength):
                    if (graphicalUiElement is LineRuntime lineDashRuntime)
                    {
                        lineDashRuntime.StrokeDashLength = (float)value;
                    }
                    else
                    {
                        asLine.StrokeDashLength = (float)value;
                    }
                    handled = true;
                    break;
                case nameof(LineRuntime.StrokeGapLength):
                    if (graphicalUiElement is LineRuntime lineGapRuntime)
                    {
                        lineGapRuntime.StrokeGapLength = (float)value;
                    }
                    else
                    {
                        asLine.StrokeGapLength = (float)value;
                    }
                    handled = true;
                    break;
            }
            if (!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asLine, graphicalUiElement, propertyName, value);
            }
            if (!handled)
            {
                handled = TrySetPropertyOnLine(asLine, graphicalUiElement, propertyName, value);
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
                case nameof(ColoredCircleRuntime.StrokeDashLength):
                    ((ColoredCircleRuntime)graphicalUiElement).StrokeDashLength = (float)value;
                    handled = true;
                    break;
                case nameof(ColoredCircleRuntime.StrokeGapLength):
                    ((ColoredCircleRuntime)graphicalUiElement).StrokeGapLength = (float)value;
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
            // VectorSprite is not a RenderableShapeBase, so it stays on its dedicated handler
            // plus the reflection fallback. No shared shape-base properties (Blend, etc.) apply.
            if(!handled)
            {
                handled = TrySetPropertyOnSvg(asSvg, graphicalUiElement, propertyName, value);
            }
        }
        else if(containedObjectAsIpso is Sprite asSprite)
        {
            // Route base-class properties (Blend, gradient/dropshadow channels, etc.) first.
            // These branches historically skipped TrySetPropertiesOnRenderableBase and relied
            // on the reflection fallback, which works for int/float properties but throws on
            // enum -> Nullable<enum> (the Blend case).
            if(!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asSprite, graphicalUiElement, propertyName, value);
            }
            if(!handled)
            {
                handled = TrySetPropertyOnSprite(asSprite, graphicalUiElement, propertyName, value);
            }
        }
        else if(containedObjectAsIpso is NineSlice asNineSlice)
        {
            if(!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asNineSlice, graphicalUiElement, propertyName, value);
            }
            if(!handled)
            {
                handled = TrySetPropertyOnNineSlice(asNineSlice, graphicalUiElement, propertyName, value);
            }
        }
        else if(containedObjectAsIpso is Polygon asPolygon)
        {
            if(!handled)
            {
                handled = TrySetPropertiesOnRenderableBase(asPolygon, graphicalUiElement, propertyName, value);
            }
            if(!handled)
            {
                handled = TrySetPropertyOnPolygon(asPolygon, graphicalUiElement, propertyName, value);
            }
        }
        // Catch-all for RenderableShapeBase derivatives that have no dedicated branch
        // above (SolidRectangle, LineRectangle, LineGrid, etc.). Without this, those types
        // skip TrySetPropertiesOnRenderableBase entirely and any RenderableShapeBase property
        // — most notably Blend? — falls through to SetPropertyThroughReflection's
        // Convert.ChangeType, which throws on enum -> Nullable<enum>.
        if (!handled && containedObjectAsIpso is RenderableShapeBase asShapeBase)
        {
            handled = TrySetPropertiesOnRenderableBase(asShapeBase, graphicalUiElement, propertyName, value);
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

    private static bool TrySetPropertyOnNineSlice(NineSlice asNineSlice, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        switch(propertyName)
        {
            case "SourceFile":
                var asString = value as string;
                if (!string.IsNullOrEmpty(asString))
                {
                    var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                    var contentLoader = loaderManager.ContentLoader;
                    asNineSlice.Texture = contentLoader.LoadContent<SKBitmap>(asString);
                }
                else
                {
                    asNineSlice.Texture = null;
                }
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

        if (propertyName == "Text" || propertyName == "TextNoTranslate")
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
