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

            return toReturn;
        }

        public static void SetGraphicalUiElement(this ElementSave elementSave, GraphicalUiElement toReturn, SystemManagers systemManagers)
        {
            RecursiveVariableFinder rvf = new RecursiveVariableFinder(elementSave.DefaultState);

            InstanceSaveExtensionMethods.SetGraphicalUiElement(rvf, elementSave.BaseType,
                ref toReturn, systemManagers);

            foreach (var variable in elementSave.DefaultState.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
            {
                toReturn.AddExposedVariable(variable.ExposedAsName, variable.Name);
            }

            toReturn.AddStates(elementSave.States);

            toReturn.Tag = elementSave;

            bool isScreen = elementSave is ScreenSave;

            foreach (var instance in elementSave.Instances)
            {
                var childGue = instance.ToGraphicalUiElement(systemManagers);



                if (!isScreen)
                {

                    childGue.Parent = toReturn;
                }
                childGue.ParentGue = toReturn;

                var state = rvf.GetValue<string>(childGue.Name + ".State");

                if (!string.IsNullOrEmpty(state) && state != "Default")
                {
                    childGue.ApplyState(state);
                }
            }


        }



    }
}
