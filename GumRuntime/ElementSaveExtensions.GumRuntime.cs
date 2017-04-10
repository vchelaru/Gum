using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GumRuntime
{
    public static class ElementSaveExtensions
    {
        static Dictionary<string, Type> mElementToGueTypes = new Dictionary<string, Type>();

        public static void RegisterGueInstantiationType(string elementName, Type gueInheritingType)
        {
            mElementToGueTypes[elementName] = gueInheritingType;
        }

        public static GraphicalUiElement CreateGueForElement(ElementSave elementSave, bool fullInstantiation = false)
        {
            GraphicalUiElement toReturn = null;


            if (mElementToGueTypes.ContainsKey(elementSave.Name))
            {
                var type = mElementToGueTypes[elementSave.Name];
                var constructor = type.GetConstructor(new Type[] { typeof(bool), typeof(bool) });


                bool callAssignReferences = fullInstantiation;
                toReturn = constructor.Invoke(new object[] { fullInstantiation, callAssignReferences }) as GraphicalUiElement;
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
            GraphicalUiElement toReturn = CreateGueForElement(elementSave);

            elementSave.SetGraphicalUiElement(toReturn, systemManagers);

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

        public static void CreateGraphicalComponent(this GraphicalUiElement graphicalElement, ElementSave elementSave, SystemManagers systemManagers)
        {
            IRenderable containedObject = null;

            bool handled = InstanceSaveExtensionMethods.TryHandleAsBaseType(elementSave.Name, systemManagers, out containedObject);

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
                        graphicalElement.CreateGraphicalComponent(baseElement, systemManagers);
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


        static void CreateChildrenRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave, SystemManagers systemManagers)
        {
            bool isScreen = elementSave is ScreenSave;

            foreach (var instance in elementSave.Instances)
            {
                var childGue = instance.ToGraphicalUiElement(systemManagers);

                if (childGue != null)
                {
                    if (!isScreen)
                    {
                        childGue.Parent = graphicalElement;
                    }
                    childGue.ParentGue = graphicalElement;
                }
            }
        }

        static void SetVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        {
            graphicalElement.SetVariablesRecursively(elementSave, elementSave.DefaultState);
        }
        
        public static void SetVariablesTopLevel(this GraphicalUiElement graphicalElement, ElementSave elementSave, Gum.DataTypes.Variables.StateSave stateSave)
        {
            foreach (var variable in stateSave.Variables.Where(item => item.SetsValue && item.Value != null))
            {
                graphicalElement.SetProperty(variable.Name, variable.Value);
            }
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

            var variablesToSet = stateSave.Variables
                .Where(item => item.SetsValue && item.Value != null)
                // States should be applied first, then values may override states (order by sorts false first):
                .OrderBy(item=>!item.IsState(elementSave));

            foreach (var variable in variablesToSet)
            {
                // See below for explanation on why we don't set Parent here
                if (variable.GetRootName() != "Parent")
                {
                    graphicalElement.SetProperty(variable.Name, variable.Value);
                }
            }

            // Now set all parents
            // The reason for this is
            // because parents need to
            // be assigned in the order
            // of the instances in the .glux.
            // That way they are drawn in the same
            // order as they are defined.
            variablesToSet = variablesToSet.Where(item => item.GetRootName() == "Parent")
                .OrderBy(item => elementSave.Instances.FindIndex(instance => instance.Name == item.SourceObject));

            foreach (var variable in variablesToSet)
            {
                graphicalElement.SetProperty(variable.Name, variable.Value);
            }
        }

        public static void SetGraphicalUiElement(this ElementSave elementSave, GraphicalUiElement toReturn, SystemManagers systemManagers)
        {
            // We need to set categories and states first since those are used below;
            toReturn.SetStatesAndCategoriesRecursively(elementSave);

            toReturn.CreateGraphicalComponent(elementSave, systemManagers);

            toReturn.AddExposedVariablesRecursively(elementSave);

            toReturn.CreateChildrenRecursively(elementSave, systemManagers);

            toReturn.Tag = elementSave;

            toReturn.SetVariablesRecursively(elementSave);
        }



    }
}
