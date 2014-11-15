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

            ElementSave instanceElement = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

            GraphicalUiElement toReturn = null;
            if (instanceElement != null)
            {
                toReturn = ElementSaveExtensions.CreateGueForElement(instanceElement);

                GumRuntime.ElementSaveExtensions.SetGraphicalUiElement(instanceElement, toReturn, systemManagers);

                toReturn.Name = instanceSave.Name;
                toReturn.Tag = instanceSave;

                var state = instanceSave.ParentContainer.DefaultState;



                foreach (var variable in state.Variables.Where(item => item.SourceObject == instanceSave.Name))
                {
                    string propertyOnInstance = variable.Name.Substring(variable.Name.LastIndexOf('.') + 1);

                    if (toReturn.IsExposedVariable(propertyOnInstance))
                    {
                        toReturn.SetProperty(propertyOnInstance, variable.Value);
                    }
                }
            }

            return toReturn;

        }



        internal static bool TryHandleAsBaseType(string baseType, SystemManagers systemManagers, out IRenderable containedObject)
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
                    containedObject = solidRectangle;
                    break;
                case "Sprite":
                    Texture2D texture = null;

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
                        Text text = new Text(systemManagers, "");

                        //BitmapFont font = null;
                        //if (rvf.GetValue<bool>("UseCustomFont"))
                        //{
                        //    string customFontFile = rvf.GetValue<string>("CustomFontFile");

                        //    if (!string.IsNullOrEmpty(customFontFile))
                        //    {
                        //        font = new BitmapFont(customFontFile, systemManagers);
                        //    }


                        //}
                        //else
                        //{

                        //    string fontName = rvf.GetValue<string>("Font");
                        //    int fontSize = rvf.GetValue<int>("FontSize"); // verify these var names
                        //    fontName = "FontCache/Font" + fontSize.ToString() + fontName + ".fnt";
                        //    font = new BitmapFont(fontName, systemManagers);
                        //}

                        containedObject = text;

                        //Do Text specific alignment.
                        //SetAlignmentValues(text, rvf);
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
