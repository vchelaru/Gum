using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
#if !NO_XNA
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
#endif
using System;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;

namespace GumRuntime
{
    public static class InstanceSaveExtensionMethods
    {

#if !NO_XNA
        public static Func<string, IRenderable> CustomObjectCreation;
        public static GraphicalUiElement ToGraphicalUiElement(this InstanceSave instanceSave, SystemManagers systemManagers)
        {
#if DEBUG
            if(ObjectFinder.Self.GumProjectSave == null)
            {
                throw new InvalidOperationException("You need to set the ObjectFinder's GumProjectSave first so it can track references");
            }
#endif
            ElementSave instanceElement = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

            GraphicalUiElement toReturn = null;
            if (instanceElement != null)
            {
                string genericType = null;


                if(instanceElement.Name == "Container" && instanceElement is StandardElementSave)
                {
                    genericType = instanceSave.ParentContainer.DefaultState.GetValueOrDefault<string>(instanceSave.Name + "." + "Contained Type");
                }

                toReturn = ElementSaveExtensions.CreateGueForElement(instanceElement, true, genericType);

                // If we get here but there's no contained graphical object then that means we don't
                // have a strongly-typed system (like when a game is running in FRB). Therefore, we'll
                // just fall back to the regular creation of graphical objects, like is done in the Gum tool:
                if(toReturn.RenderableComponent == null)
                {
                    instanceElement.SetGraphicalUiElement(toReturn, systemManagers);
                }


                toReturn.Name = instanceSave.Name;
                toReturn.Tag = instanceSave;


                // November 9, 2020 - Vic asks - why do we set properties here, and ONLY exposed variables? That's weird...
                // It also requires using instanceSave.ParentContainer, and maybe is duplicate setting values which hurts performance
                // andn adds complexity. Not sure what to do here..., but going to try commenting it out to see if it makes a difference
                //var instanceContainerDefaultState = instanceSave.ParentContainer.DefaultState;

                //foreach (var variable in instanceContainerDefaultState.Variables.Where(item => item.SetsValue && item.SourceObject == instanceSave.Name))
                //{
                //    string propertyOnInstance = variable.Name.Substring(variable.Name.LastIndexOf('.') + 1);

                //    if (toReturn.IsExposedVariable(propertyOnInstance))
                //    {
                //        toReturn.SetProperty(propertyOnInstance, variable.Value);
                //    }
                //}
            }

            return toReturn;

        }


        internal static bool TryHandleAsBaseType(string baseType, SystemManagers systemManagers, out IRenderable containedObject)
        {
            bool handledAsBaseType = true;
            containedObject = null;
#if MONOGAME
            switch (baseType)
            {

                case "Container":
                case "Component": // this should never be set in Gum, but there could be XML errors or someone could have used an old Gum...
                    if(GraphicalUiElement.ShowLineRectangles)
                    {
                        LineRectangle lineRectangle = new LineRectangle(systemManagers);
                        lineRectangle.Color = Color.FromArgb(
                            255,
#if GUM
                            Gum.ToolStates.GumState.Self.ProjectState.GeneralSettings.OutlineColorR,
                            Gum.ToolStates.GumState.Self.ProjectState.GeneralSettings.OutlineColorG,
                            Gum.ToolStates.GumState.Self.ProjectState.GeneralSettings.OutlineColorB
#else
                        255,255,255,255
#endif
                            );

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
                        containedObject = text;
                    }
                    break;


                default:
                    containedObject = CustomObjectCreation?.Invoke(baseType);
                    handledAsBaseType = containedObject != null;
                    break;
            }
#endif
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
            Color color = ColorFromRvf(rvf);
            text.Red = color.R;
            text.Green = color.G;
            text.Blue = color.B;
            text.Alpha = color.A;  //Is alpha supported?
        }

        static Color ColorFromRvf(RecursiveVariableFinder rvf)
        {
            Color color = Color.FromArgb(
                rvf.GetValue<int>("Alpha"),
                rvf.GetValue<int>("Red"),
                rvf.GetValue<int>("Green"),
                rvf.GetValue<int>("Blue")
                );
            return color;
        }

        private static void SetAlignmentValues(Text text, RecursiveVariableFinder rvf)
        {
            //Could these potentially be out of bounds?
            text.HorizontalAlignment = rvf.GetValue<HorizontalAlignment>("HorizontalAlignment");
            text.VerticalAlignment = rvf.GetValue<VerticalAlignment>("VerticalAlignment");
        }
#endif
    }
}
