using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if GUM
using Gum.Services;
using Gum.ToolStates;
#endif

#if RAYLIB
using Gum.Renderables;
using Raylib_cs;

#else
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math.Geometry;
#endif

namespace Gum.Wireframe
{
    public static class RuntimeObjectCreator
    {

        public static IRenderable TryHandleAsBaseType(string baseType, ISystemManagers managers)
        {
            var systemManagers = managers as SystemManagers;
            IRenderable containedObject = null;
            switch (baseType)
            {
#if MONOGAME || KNI || FNA || RAYLIB

                case "Container":
                case "Component": // this should never be set in Gum, but there could be XML errors or someone could have used an old Gum...

                    var showComponentLineRectangles = GraphicalUiElement.ShowLineRectangles;

#if RAYLIB
                    showComponentLineRectangles = false;
#endif

                    if (showComponentLineRectangles)
                    {
                        LineRectangle lineRectangle = new LineRectangle(systemManagers);
                        lineRectangle.Color = System.Drawing.Color.FromArgb(255,255,255,255);
#if GUM
                        lineRectangle.IsDotted = true;

                        var projectState = Locator.GetRequiredService<IProjectState>();
                        lineRectangle.Color = System.Drawing.Color.FromArgb(
                            255,
                            projectState.GeneralSettings.OutlineColorR,
                            projectState.GeneralSettings.OutlineColorG,
                            projectState.GeneralSettings.OutlineColorB
                            );
#endif
                        containedObject = lineRectangle;
                    }
                    else
                    {
                        containedObject = new InvisibleRenderable();
                    }
                    break;

                case "Rectangle":
                    LineRectangle rectangle = new LineRectangle(systemManagers);
                    rectangle.IsDotted = false;
                    containedObject = rectangle;
                    break;
                case "Circle":
                    LineCircle circle = new LineCircle(systemManagers);
                    circle.CircleOrigin = CircleOrigin.TopLeft;
                    containedObject = circle;
                    break;
                case "Polygon":
                    LinePolygon polygon = new LinePolygon(systemManagers);
                    containedObject = polygon;
                    break;
                case "ColoredRectangle":
                    SolidRectangle solidRectangle = new SolidRectangle();
                    containedObject = solidRectangle;
                    break;
                case "Sprite":
                    Texture2D? texture = null;

                    Sprite sprite = new Sprite(texture);
                    containedObject = sprite;

                    break;
                case "NineSlice":
                    {
                        NineSlice nineSlice = new NineSlice();
                        containedObject = nineSlice;
                    }
                    break;
                case "Text":
                    {
                        Text text = new Text(systemManagers);
                        text.RawText = string.Empty;
                        containedObject = text;
                    }
                    break;
#endif

#if SKIA
                case "Arc":
                    return new SkiaGum.Renderables.Arc();
                case "ColoredCircle":
                    return new SkiaGum.Renderables.Circle();
                case "RoundedRectangle":
                    return new SkiaGum.Renderables.RoundedRectangle();

#endif


            }
            return containedObject;
        }

    }
}
