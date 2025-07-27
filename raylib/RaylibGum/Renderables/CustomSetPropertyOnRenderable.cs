using Gum.DataTypes;
using Gum.Renderables;
using Gum.Wireframe;
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
    public static void SetPropertyOnRenderable(IRenderableIpso mContainedObjectAsIpso, GraphicalUiElement graphicalUiElement, string propertyName, object value)
    {
        bool handled = false;

        if (mContainedObjectAsIpso is Text asText)
        {
            handled = TrySetPropertyOnText(asText, graphicalUiElement, propertyName, value);
        }

        if (!handled)
        {
            GraphicalUiElement.SetPropertyThroughReflection(mContainedObjectAsIpso, graphicalUiElement, propertyName, value);
            //SetPropertyOnRenderable(mContainedObjectAsIpso, propertyName, value);
        }
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
