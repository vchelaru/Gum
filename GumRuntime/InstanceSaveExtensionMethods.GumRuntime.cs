using Gum.DataTypes;
using Gum.Managers;
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
        public static GraphicalUiElement ToGraphicalUiElement(this InstanceSave instanceSave, SystemManagers systemManagers)
        {
            GraphicalUiElement toReturn = new GraphicalUiElement(
                null, null);

            RecursiveVariableFinder rvf = new RecursiveVariableFinder(instanceSave, instanceSave.ParentContainer);
            SetGraphicalUiElement(rvf, instanceSave.BaseType, ref toReturn, systemManagers);
            toReturn.Name = instanceSave.Name;
            return toReturn;

        }

        public static void SetGraphicalUiElement(RecursiveVariableFinder rvf, string baseType, ref GraphicalUiElement graphicalUiElement, SystemManagers systemManagers)
        {
            IRenderable containedObject = null;

            bool handled = TryHandleAsBaseType(rvf, baseType, systemManagers, out containedObject);

            if (handled)
            {
                graphicalUiElement.SetContainedObject(containedObject);
            }
            else
            {
                ElementSave elementSave = ObjectFinder.Self.GetElementSave(baseType);

                if (elementSave != null && elementSave is ComponentSave)
                {
                    ElementSaveExtensions.SetGraphicalUiElement(elementSave, graphicalUiElement, systemManagers);
                }
            }
            graphicalUiElement.SetGueWidthAndPositionValues(rvf);
        }

        private static bool TryHandleAsBaseType(RecursiveVariableFinder rvf, string baseType, SystemManagers systemManagers, out IRenderable containedObject)
        {
            bool handledAsBaseType = true;
            containedObject = null;

            switch (baseType)
            {

                case "Container":

                    LineRectangle lineRectangle = new LineRectangle(systemManagers);
                    containedObject = lineRectangle;
                    break;

                case "ColoredRectangle":
                    SolidRectangle solidRectangle = new SolidRectangle();
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
                    SetAlphaAndColorValues(sprite, rvf);
                    sprite.Visible = rvf.GetValue<bool>("Visible");

                    //Sprite specific
                    sprite.FlipHorizontal = rvf.GetValue<bool>("FlipHorizontal");
                    sprite.FlipVertical = rvf.GetValue<bool>("FlipVertical");
                    containedObject = sprite;

                    break;
                case "NineSlice":
                    {
                        string file = rvf.GetValue<string>("SourceFile");
                        NineSlice nineSlice = new NineSlice();
                        string relativeFile = rvf.GetValue<string>("SourceFile");
                        nineSlice.SetTexturesUsingPattern(relativeFile, systemManagers);

                        SetAlphaAndColorValues(nineSlice, rvf);
                        nineSlice.Visible = rvf.GetValue<bool>("Visible");
                        containedObject = nineSlice;

                    }
                    break;
                case "Text":
                    {
                        Text text = new Text(systemManagers, rvf.GetValue<string>("Text"));

                        SetAlphaAndColorValues(text, rvf);
                        text.Visible = rvf.GetValue<bool>("Visible");

                        string fontName = rvf.GetValue<string>("Font");
                        int fontSize = rvf.GetValue<int>("FontSize"); // verify these var names
                        fontName = "FontCache/Font" + fontSize.ToString() + fontName + ".fnt";
                        BitmapFont font = new BitmapFont(fontName, systemManagers);
                        text.BitmapFont = font;

                        containedObject = text;

                        //Do Text specific alignment.
                        SetAlignmentValues(text, rvf);
                    }
                    break;
                default:
                    handledAsBaseType = false;
                    break;
            }



            return handledAsBaseType;

        }


        private static void SetAlphaAndColorValues(SolidRectangle solidRectangle, RecursiveVariableFinder rvf)
        {
            solidRectangle.Color = ColorFromRvf(rvf);
        }

        private static void SetAlphaAndColorValues(Sprite sprite, RecursiveVariableFinder rvf)
        {
            sprite.Color = ColorFromRvf(rvf);
        }

        private static void SetAlphaAndColorValues(NineSlice nineSlice, RecursiveVariableFinder rvf)
        {
            nineSlice.Color = ColorFromRvf(rvf);
        }

        private static void SetAlphaAndColorValues(Text text, RecursiveVariableFinder rvf)
        {
            Microsoft.Xna.Framework.Color color = ColorFromRvf(rvf);
            text.Red = color.R;
            text.Green = color.G;
            text.Blue = color.B;
            text.Alpha = color.A;  //Is alpha supported?
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

        private static void SetAlignmentValues(Text text, RecursiveVariableFinder rvf)
        {
            //Could these potentially be out of bounds?
            text.HorizontalAlignment = rvf.GetValue<HorizontalAlignment>("HorizontalAlignment");
            text.VerticalAlignment = rvf.GetValue<VerticalAlignment>("VerticalAlignment");
        }
    }
}
