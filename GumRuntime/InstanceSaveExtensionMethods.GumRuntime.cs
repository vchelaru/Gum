using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
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
        public static GraphicalUiElement ToGraphicalUiElement(this InstanceSave instanceSave, ISystemManagers systemManagers)
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
            }

            return toReturn;

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
