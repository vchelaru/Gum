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


        public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers systemManagers,
            bool addToManagers)
        {
            return elementSave.ToGraphicalUiElement(systemManagers, addToManagers, new RecursiveVariableFinder(elementSave.DefaultState));

        }

        public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers systemManagers, 
            bool addToManagers, RecursiveVariableFinder rvf)
        {
            GraphicalUiElement toReturn = CreateGueForElement(elementSave);

            elementSave.SetGraphicalUiElement(toReturn, systemManagers, rvf);

            //no layering support yet
            if (addToManagers)
            {
                toReturn.AddToManagers(systemManagers, null);
            }

            return toReturn;
        }

        public static void SetGraphicalUiElement(this ElementSave elementSave, GraphicalUiElement toReturn, SystemManagers systemManagers, RecursiveVariableFinder rvf)
        {

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

            InstanceSave instanceToRestore = null;

            if (rvf.ContainerType == RecursiveVariableFinder.VariableContainerType.InstanceSave)
            {
                instanceToRestore = rvf.PopInstance();

                var toPush = new ElementWithState(elementSave);
                toPush.InstanceName = instanceToRestore.Name;

                rvf.PushElement(toPush);
            }


            foreach (var instance in elementSave.Instances)
            {
                rvf.PushInstance(instance);

                var childGue = instance.ToGraphicalUiElement(systemManagers, rvf);

                if (childGue != null)
                {
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

                rvf.PopInstance();
            }


            // instances have been created, let's do the attachment:
            foreach (var instance in elementSave.Instances)
            {
                rvf.PushInstance(instance);
                var parentValue = rvf.GetValue<string>("Parent");

                if (!string.IsNullOrEmpty(parentValue))
                {
                    var instanceGue = toReturn.GetGraphicalUiElementByName(instance.Name);

                    if (instanceGue != null)
                    {
                        var potentialParent = toReturn.GetGraphicalUiElementByName(parentValue);
                        if (potentialParent != null)
                        {
                            instanceGue.Parent = potentialParent;
                        }
                    }
                }
                rvf.PopInstance();
            }


            if (instanceToRestore != null)
            {
                rvf.PopElement();
                rvf.PushInstance(instanceToRestore);
            }
        }



    }
}
