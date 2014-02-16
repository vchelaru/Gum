using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary;
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
            }

            graphicalUiElement.SetContainedObject(containedObject);
            graphicalUiElement.SetGueWidthAndPositionValues(rvf);
        }


        private static void SetAlphaAndColorValues(SolidRectangle solidRectangle, RecursiveVariableFinder rvf)
        {
            Microsoft.Xna.Framework.Color color = new Microsoft.Xna.Framework.Color(
                rvf.GetValue<int>("Red"),
                rvf.GetValue<int>("Green"),
                rvf.GetValue<int>("Blue"),
                rvf.GetValue<int>("Alpha")

                );
            solidRectangle.Color = color;
        }


    }
}
