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
                var type = mElementToGueTypes[elementSave.Name];
                var constructor = type.GetConstructor(new Type[]{typeof(bool)});
                bool fullInstantiation = false;
                toReturn = constructor.Invoke(new object[]{fullInstantiation}) as GraphicalUiElement;
            }
            else
            {
                toReturn = new GraphicalUiElement();
            }
            toReturn.ElementSave = elementSave;
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

        public static void SetStatesAndCategoriesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        {

            if(!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if(baseElementSave != null)
                {
                    graphicalElement.SetStatesAndCategoriesRecursively(baseElementSave);
                }
            }

            // We need to set categories and states before calling SetGraphicalUiElement so that the states can be used
            foreach (var category in elementSave.Categories)
            {
                graphicalElement.AddCategory(category);
            }

            graphicalElement.AddStates(elementSave.States);
        }

        public static void CreateGraphicalComponent(this GraphicalUiElement graphicalElement, RecursiveVariableFinder rvf, ElementSave elementSave, SystemManagers systemManagers)
        {
            IRenderable containedObject = null;

            bool handled = InstanceSaveExtensionMethods.TryHandleAsBaseType(rvf, elementSave.Name, systemManagers, out containedObject);

            if (handled)
            {
                graphicalElement.SetContainedObject(containedObject);
            }
            else
            {
                if (elementSave != null && elementSave is ComponentSave)
                {
                    var baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);

                    if (baseElement != null)
                    {
                        graphicalElement.CreateGraphicalComponent(rvf, baseElement, systemManagers);
                    }
                }
            }
        }

        static void AddExposedVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        {
            if (!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if (baseElementSave != null)
                {
                    graphicalElement.AddExposedVariablesRecursively(baseElementSave);
                }
            }


            if (elementSave != null)
            {
                foreach (var variable in elementSave.DefaultState.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
                {
                    graphicalElement.AddExposedVariable(variable.ExposedAsName, variable.Name);
                }
            }

        }


        static void CreateChildrenRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave, RecursiveVariableFinder rvf, SystemManagers systemManagers)
        {
            bool isScreen = elementSave is ScreenSave;

            foreach (var instance in elementSave.Instances)
            {
                rvf.PushInstance(instance);

                var childGue = instance.ToGraphicalUiElement(systemManagers, rvf);

                if (childGue != null)
                {
                    if (!isScreen)
                    {
                        childGue.Parent = graphicalElement;
                    }
                    childGue.ParentGue = graphicalElement;

                    // I think we just pass "State"
                    //var state = rvf.GetValue<string>(childGue.Name + ".State");
                    var state = rvf.GetValue<string>("State");

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
                    var instanceGue = graphicalElement.GetGraphicalUiElementByName(instance.Name);

                    if (instanceGue != null)
                    {
                        var potentialParent = graphicalElement.GetGraphicalUiElementByName(parentValue);
                        if (potentialParent != null)
                        {
                            instanceGue.Parent = potentialParent;
                        }
                    }
                }
                rvf.PopInstance();
            }

        }

        static void SetVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        {
            graphicalElement.SetVariablesRecursively(elementSave, elementSave.DefaultState);
        }
        
        public static void SetVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave, Gum.DataTypes.Variables.StateSave stateSave)
        {
            if (!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if (baseElementSave != null)
                {
                    graphicalElement.SetVariablesRecursively(baseElementSave);
                }
            }

            foreach (var variable in stateSave.Variables.Where(item => item.SetsValue && item.Value != null))
            {
                graphicalElement.SetProperty(variable.Name, variable.Value);
            }
        }

        public static void SetGraphicalUiElement(this ElementSave elementSave, GraphicalUiElement toReturn, SystemManagers systemManagers, RecursiveVariableFinder rvf)
        {
            // We need to set categories and states first since those are used below;
            toReturn.SetStatesAndCategoriesRecursively(elementSave);

            toReturn.CreateGraphicalComponent(rvf, elementSave, systemManagers);

            toReturn.AddExposedVariablesRecursively(elementSave);

            toReturn.CreateChildrenRecursively(elementSave, rvf, systemManagers);

            toReturn.Tag = elementSave;

            toReturn.SetVariablesRecursively(elementSave);
        }



    }
}
