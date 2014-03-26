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
    public static class ElementSaveExtensions
    {
        public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers systemManagers, bool addToManagers)
        {

            GraphicalUiElement toReturn = new GraphicalUiElement(
                null, null);


            elementSave.SetGraphicalUiElement(toReturn, systemManagers);

            //no layering support yet
            if (addToManagers)
            {
                toReturn.AddToManagers(systemManagers, null);
            }

            foreach (var instance in elementSave.Instances)
            {
                var childGue = instance.ToGraphicalUiElement(systemManagers);
                
                //no layering support yet
                if (addToManagers)
                {
                    childGue.AddToManagers(systemManagers, null);
                }


                childGue.Parent = toReturn;
            }


            return toReturn;
        }

        public static void SetGraphicalUiElement(this ElementSave elementSave, GraphicalUiElement toReturn, SystemManagers systemManagers)
        {
            RecursiveVariableFinder rvf = new RecursiveVariableFinder(elementSave.DefaultState);

            InstanceSaveExtensionMethods.SetGraphicalUiElement(rvf, elementSave.BaseType,
                toReturn, systemManagers);
        }



    }
}
