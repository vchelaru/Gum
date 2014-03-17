using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumRuntime
{
    public static class InstanceSaveExtensionMethods
    {
        public static GraphicalUiElement ToGraphicalUiElement(this InstanceSave instanceSave, SystemManagers systemManagers, bool addToManagers)
        {
            GraphicalUiElement toReturn = new GraphicalUiElement(
                null, null);

            instanceSave.SetGraphicalUiElement(toReturn, systemManagers, addToManagers);

            return toReturn;

        }

        public static void SetGraphicalUiElement(this InstanceSave instanceSave, GraphicalUiElement graphicalUiElement, SystemManagers systemManagers, bool addToManagers)
        {

            RecursiveVariableFinder rvf = new RecursiveVariableFinder(instanceSave, instanceSave.ParentContainer);
            SetGraphicalUiElement(rvf, instanceSave.BaseType, graphicalUiElement, systemManagers, addToManagers);

        }

        public static void SetGraphicalUiElement(RecursiveVariableFinder rvf, string baseType, GraphicalUiElement graphicalUiElement, SystemManagers systemManagers, bool addToManagers)
        {
            IRenderable containedObject = null;

            switch (baseType)
            {

                case "Container":

                    LineRectangle lineRectangle = new LineRectangle(systemManagers);
                    if (addToManagers)
                    {
                        systemManagers.ShapeManager.Add(lineRectangle);
                    }
                    containedObject = lineRectangle;
                    break;

                case "ColoredRectangle":
                    SolidRectangle solidRectangle = new SolidRectangle();
                    if (addToManagers)
                    {
                        systemManagers.ShapeManager.Add(solidRectangle);
                    }
                    SetAlphaAndColorValues(solidRectangle, rvf);
                    solidRectangle.Visible = rvf.GetValue<bool>("Visible");
                    containedObject = solidRectangle;
                    break;
                case "Sprite":
                    Texture2D texture = null;

                    string textureValue = rvf.GetValue<string>("SourceFile");
                    if (!string.IsNullOrEmpty(textureValue))
                    {
                        texture = LoaderManager.Self.Load(textureValue, systemManagers);
                    }
                    Sprite sprite = new Sprite(texture);
                    if (addToManagers)
                    {
                        systemManagers.SpriteManager.Add(sprite);
                    }
                    SetAlphaAndColorValues(sprite, rvf);
                    sprite.Visible = rvf.GetValue<bool>("Visible");
                    containedObject = sprite;

                    break;
                case "NineSlice":
                    {
                        string file = rvf.GetValue<string>("SourceFile");
                        NineSlice nineSlice = new NineSlice();
                        string relativeFile = rvf.GetValue<string>("SourceFile");
                        nineSlice.SetTexturesUsingPattern(relativeFile, systemManagers);

                        if (addToManagers)
                        {
                            systemManagers.SpriteManager.Add(nineSlice);
                        }
                        // set alpha and color?
                        nineSlice.Visible = rvf.GetValue<bool>("Visible");
                        containedObject = nineSlice;

                    }
                    break;
                default:

                    throw new Exception("The following type is not supported: " + baseType);
            }

            graphicalUiElement.SetContainedObject(containedObject);
            graphicalUiElement.SetGueWidthAndPositionValues(rvf);
        }


        private static void SetAlphaAndColorValues(SolidRectangle solidRectangle, RecursiveVariableFinder rvf)
        {
            solidRectangle.Color = ColorFromRvf(rvf);
        }

        private static void SetAlphaAndColorValues(Sprite sprite, RecursiveVariableFinder rvf)
        {
            sprite.Color = ColorFromRvf(rvf);
        }


        static Microsoft.Xna.Framework.Color ColorFromRvf(RecursiveVariableFinder rvf)
        {
            Microsoft.Xna.Framework.Color color = new Microsoft.Xna.Framework.Color(
                rvf.GetValue<int>("Red"),
                rvf.GetValue<int>("Green"),
                rvf.GetValue<int>("Blue"),
                rvf.GetValue<int>("Alpha")
                );
            return color;
        }
    }
}
