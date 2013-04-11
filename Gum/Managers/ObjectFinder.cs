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
                    if (screenSave.Name == screenName)
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
                    if (componentSave.Name == componentName)
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
        public List<ElementSave> GetElementsReferencing(ElementSave elementSave, List<ElementSave> list = null)
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
                        break;
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
                        break;
                    }
                }
            }



            return list;
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

        public List<string> GetAllFilesInProject()
        {
            List<string> toReturn = new List<string>();

            FillListWithReferencedFiles(toReturn, GumProjectSave.Screens);
            FillListWithReferencedFiles(toReturn, GumProjectSave.Components);
            FillListWithReferencedFiles(toReturn, GumProjectSave.StandardElements);

            StringFunctions.RemoveDuplicates(toReturn);
            return toReturn;
        }

        void FillListWithReferencedFiles<T>(List<string> files, IList<T> elements) where T : ElementSave
        {
            RecursiveVariableFinder rvf;
            string value;


            // These files are all relative to the project, so we don't have to worry
            // about making them absolute/relative again.  It should just work.
            foreach (ElementSave element in elements)
            {
                rvf = new RecursiveVariableFinder(element.DefaultState);

                value = rvf.GetValue("SourceFile") as string;

                if (!string.IsNullOrEmpty(value))
                {
                    files.Add(value);
                }

                foreach (InstanceSave instance in element.Instances)
                {
                    rvf = new RecursiveVariableFinder(instance, element);

                    value = rvf.GetValue("SourceFile") as string;

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


    }
}
