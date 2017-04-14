using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.Managers
{
    public class ObjectFinder
    {
        #region Fields

        static ObjectFinder mObjectFinder;

        #endregion

        public static ObjectFinder Self
        {
            get
            {
                if (mObjectFinder == null)
                {
                    mObjectFinder = new ObjectFinder();
                }
                return mObjectFinder;
            }
        }

        public GumProjectSave GumProjectSave
        {
            get;
            set;
        }


        public ScreenSave GetScreen(string screenName)
        {
            GumProjectSave gps = GumProjectSave;

            if (gps != null)
            {
                foreach (ScreenSave screenSave in gps.Screens)
                {
                    // Since the screen name may come from a file we want to ignore case:
                    //if (screenSave.Name == screenName)
                    if (screenSave.Name.Equals(screenName, StringComparison.OrdinalIgnoreCase))
                    {
                        return screenSave;
                    }
                }

            }

            return null;
        }

        public ComponentSave GetComponent(string componentName)
        {
            GumProjectSave gps = GumProjectSave;

            if (gps != null)
            {
                foreach (ComponentSave componentSave in gps.Components)
                {
                    // Since the component name may come from a file name we want
                    // to ignore case:
                    //if (componentSave.Name == componentName)
                    if (componentSave.Name.Equals(componentName, StringComparison.OrdinalIgnoreCase))
                    {
                        return componentSave;
                    }
                }

            }

            return null;
        }

        public StandardElementSave GetStandardElement(string elementName)
        {
            GumProjectSave gps = GumProjectSave;

            if (gps != null)
            {
                foreach (StandardElementSave elementSave in gps.StandardElements)
                {
                    if (elementSave.Name == elementName)
                    {
                        return elementSave;
                    }
                }

            }

            return null;
        }

        public ElementSave GetElementSave(InstanceSave instance)
        {
            return GetElementSave(instance.BaseType);
        }

        public ElementSave GetElementSave(string elementName)
        {
            ScreenSave screenSave = GetScreen(elementName);
            if (screenSave != null)
            {
                return screenSave;
            }

            ComponentSave componentSave = GetComponent(elementName);
            if (componentSave != null)
            {
                return componentSave;
            }

            StandardElementSave standardElementSave = GetStandardElement(elementName);
            if (standardElementSave != null)
            {
                return standardElementSave;
            }

            // If we got here there's nothing by the argument name
            return null;

        }

        public StandardElementSave GetRootStandardElementSave(ElementSave elementSave)
        {
            if (elementSave == null)
            {
                return null;
            }

            if (elementSave is ScreenSave)
            {
                // This will be null at the time of this writing, but may change in the future so we'll leave it here to do a proper check.

                return ObjectFinder.Self.GetElementSave("Screen") as StandardElementSave;
            }
            while (!(elementSave is StandardElementSave) && !string.IsNullOrEmpty(elementSave.BaseType))
            {
                elementSave = GetElementSave(elementSave.BaseType);
            }

            return elementSave as StandardElementSave;
        }

        public StandardElementSave GetRootStandardElementSave(InstanceSave instanceSave)
        {
            return GetRootStandardElementSave(instanceSave.GetBaseElementSave());
        }

        /// <summary>
        /// Returns a list of Elements that include InstanceSaves that use the argument
        /// elementSave as their BaseType, or that use an ElementSave deriving from elementSave
        /// as their BaseType.
        /// </summary>
        /// <param name="elementSave">The ElementSave to search for.</param>
        /// <returns>A List containing all Elements</returns>
        public List<ElementSave> GetElementsReferencing(ElementSave elementSave, List<ElementSave> list = null, List<InstanceSave> foundInstances = null)
        {
            if (list == null)
            {
                list = new List<ElementSave>();
            }

            foreach (ElementSave screen in this.GumProjectSave.Screens)
            {
                foreach (InstanceSave instanceSave in screen.Instances)
                {
                    ElementSave elementForInstance = this.GetElementSave(instanceSave.BaseType);

                    if (elementForInstance != null && elementForInstance.IsOfType(elementSave.Name))
                    {
                        list.Add(screen);

                        // If we want a list of instances
                        // then we don't want to break on a
                        // found instance - we want to continue
                        // to find all of them.
                        if (foundInstances != null)
                        {
                            foundInstances.Add(instanceSave);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            foreach (ComponentSave component in this.GumProjectSave.Components)
            {
                foreach (InstanceSave instanceSave in component.Instances)
                {
                    ElementSave elementForInstance = this.GetElementSave(instanceSave.BaseType);
                    
                    if (elementForInstance != null && elementForInstance.IsOfType(elementSave.Name))
                    {
                        list.Add(component);

                        // If we want a list of instances
                        // then we don't want to break on a
                        // found instance - we want to continue
                        // to find all of them.
                        if (foundInstances != null)
                        {
                            foundInstances.Add(instanceSave);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }



            return list;
        }

        public List<ElementSave> GetElementsReferencing(string partialFileName)
        {
            partialFileName = partialFileName.ToLower().Replace("\\", "/");

            List<ElementSave> referencingElements = new List<ElementSave>();
            foreach(var item in GumProjectSave.Screens)
            {
                List<string> files = new List<string>();

                FillListWithReferencedFiles(files, item);

                if (files.Any(file => file.ToLower().Replace("\\", "/").Contains(partialFileName)))
                {
                    referencingElements.Add(item);
                }

            }

            foreach (var item in GumProjectSave.Components)
            {
                List<string> files = new List<string>();

                FillListWithReferencedFiles(files, item);

                if (files.Any(file => file.ToLower().Replace("\\", "/").Contains(partialFileName)))
                {
                    referencingElements.Add(item);
                }

            }

            foreach (var item in GumProjectSave.StandardElements)
            {
                List<string> files = new List<string>();

                FillListWithReferencedFiles(files, item);

                if (files.Any(file => file.ToLower().Replace("\\", "/").Contains(partialFileName)))
                {
                    referencingElements.Add(item);
                }

            }

            return referencingElements;
        }

        public List<ElementSave> GetElementsReferencingRecursively(ElementSave elementSave)
        {
            List<ElementSave> typesToGoThrough = new List<ElementSave>();
            List<ElementSave> toReturn = new List<ElementSave>();
            List<ElementSave> typesFoundOnLastPass = new List<ElementSave>();

            typesToGoThrough.Add(elementSave);

            while (typesToGoThrough.Count != 0)
            {
                typesFoundOnLastPass.Clear();

                GetElementsReferencing(typesToGoThrough[0], typesFoundOnLastPass);

                foreach (var type in typesFoundOnLastPass)
                {
                    if (!toReturn.Contains(type))
                    {

                        toReturn.Add(type);
                        typesToGoThrough.Add(type);
                    }
                }


                typesToGoThrough.RemoveAt(0);
            }

            return toReturn;
        }

        public IEnumerable<string> GetAllFilesInProject()
        {
            List<string> toReturn = new List<string>();

            FillListWithReferencedFiles(toReturn, GumProjectSave.Screens);
            FillListWithReferencedFiles(toReturn, GumProjectSave.Components);
            FillListWithReferencedFiles(toReturn, GumProjectSave.StandardElements);

            return toReturn.Distinct();
        }


        private void FillListWithReferencedFiles<T>(List<string> files, IList<T> elements) where T : ElementSave
        {
            // These files are all relative to the project, so we don't have to worry
            // about making them absolute/relative again.  It should just work.
            foreach (ElementSave element in elements)
            {
                FillListWithReferencedFiles(files, element);
            }
        }

        private void FillListWithReferencedFiles(List<string> files, ElementSave element)
        {
            RecursiveVariableFinder rvf;
            string value;

            foreach (var state in element.AllStates)
            {
                rvf = new RecursiveVariableFinder(element.DefaultState);

                value = rvf.GetValue<string>("SourceFile");
                if (!string.IsNullOrEmpty(value))
                {
                    files.Add(value);
                }

                value = rvf.GetValue<string>("CustomFontFile");
                if (!string.IsNullOrEmpty(value))
                {
                    files.Add(value);
                }

                List<Gum.Wireframe.ElementWithState> elementStack = new List<Wireframe.ElementWithState>();
                var elementWithState = new Gum.Wireframe.ElementWithState(element);
                elementWithState.StateName = state.Name;
                elementStack.Add(elementWithState);

                foreach (InstanceSave instance in element.Instances)
                {
                    rvf = new RecursiveVariableFinder(instance, elementStack);

                    value = rvf.GetValue<string>("SourceFile");
                    if (!string.IsNullOrEmpty(value))
                    {
                        files.Add(value);
                    }

                    value = rvf.GetValue<string>("CustomFontFile");
                    if (!string.IsNullOrEmpty(value))
                    {
                        files.Add(value);
                    }
                }
            }
        }
    }




    public static class InstanceExtensionMethods
    {
        public static ElementSave GetBaseElementSave(this InstanceSave instanceSave)
        {
            if (string.IsNullOrEmpty(instanceSave.BaseType))
            {
                throw new InvalidOperationException("The instance with the name " + instanceSave.Name + " doesn't have a BaseType");
            }
            else
            {
                return ObjectFinder.Self.GetElementSave(instanceSave.BaseType);
            }
        }

        public static List<InstanceSave> GetSiblingsIncludingThis(this InstanceSave thisInstance)
        {
            var container = thisInstance.ParentContainer;

            List<InstanceSave> toReturn = new List<InstanceSave>();

            var defaultState = container.DefaultState;
            var thisParentValue = defaultState.GetValueOrDefault<string>($"{thisInstance.Name}.Parent");

            foreach (var instance in container.Instances)
            {
                var parentVariableName = $"{instance.Name}.Parent";
                var parentVariableValue = defaultState.GetValueOrDefault<string>(parentVariableName);

                if (thisParentValue == parentVariableValue)
                {
                    toReturn.Add(instance);
                }
            }

            return toReturn;
        }
    }
}
