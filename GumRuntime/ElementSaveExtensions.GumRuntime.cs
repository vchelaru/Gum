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
        static Dictionary<string, Type> mElementToGueTypes = new Dictionary<string, Type>();

        public static void RegisterGueInstantiationType(string elementName, Type gueInheritingType)
        {
            mElementToGueTypes[elementName] = gueInheritingType;
        }

        public static GraphicalUiElement CreateGueForElement(ElementSave elementSave)
        {

            GraphicalUiElement toReturn = null;

            if (mElementToGueTypes.ContainsKey(elementSave.Name))
            {
                var constructor = mElementToGueTypes[elementSave.Name].GetConstructor(new Type[0]);

                toReturn = constructor.Invoke(new object[0]) as GraphicalUiElement;
            }
            else
            {
                toReturn = new GraphicalUiElement();
            }
            return toReturn;
        }

        public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers systemManagers, bool addToManagers)
        {
            GraphicalUiElement toReturn = CreateGueForElement(elementSave);

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
                toReturn, systemManagers);

            foreach (var variable in elementSave.DefaultState.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
            {
                toReturn.AddExposedVariable(variable.ExposedAsName, variable.Name);
            }

            foreach (var category in elementSave.Categories)
            {
                toReturn.AddCategory(category);
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
