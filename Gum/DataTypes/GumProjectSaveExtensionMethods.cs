using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Managers;
using Gum.DataTypes.Variables;

namespace Gum.DataTypes
{
    public static class GumProjectSaveExtensionMethods
    {
        public static void Initialize(this GumProjectSave gumProjectSave)
        {
            // Do StandardElements first
            // because the values here are
            // used by components to set their
            // ignored enum values.
            foreach (StandardElementSave standardElementSave in gumProjectSave.StandardElements)
            {
                StateSave stateSave = StandardElementsManager.Self.GetDefaultStateFor(standardElementSave.Name);
                // this will result in extra variables being
                // added
                standardElementSave.Initialize(stateSave);
            }

            foreach (ScreenSave screenSave in gumProjectSave.Screens)
            {
                screenSave.Initialize(null);
            }

            

            foreach (ComponentSave componentSave in gumProjectSave.Components)
            {
                // June 27, 2012
                // We used to pass
                // null here because
                // passing a non-null
                // variable meant replacing
                // the existing StateSave with
                // the argument StateSave.  However,
                // now when the type of a Component is
                // changed, old values are not removed, but
                // are rather preserved so that changing the
                // type doesn't wipe out old values.
                //componentSave.Initialize(null);
                
                StateSave defaultStateSave = null;
                StandardElementSave ses = ObjectFinder.Self.GetRootStandardElementSave(componentSave);
                if (ses != null)
                {
                    defaultStateSave = ses.DefaultState;
                }

                componentSave.Initialize(defaultStateSave);
            }


        }

        /// <summary>
        /// Adds any Standard Elements that have been created since the project was last saved.  This should be called
        /// when the project is first loaded.
        /// </summary>
        /// <param name="gumProjectSave">The gum project to add to</param>
        public static void AddNewStandardElementTypes(this GumProjectSave gumProjectSave)
        {
            foreach(string typeName in StandardElementsManager.Self.DefaultTypes)
            {
                if (typeName != "Screen" && !gumProjectSave.StandardElements.ContainsName(typeName))
                {
                    StandardElementsManager.Self.AddStandardElementSaveInstance(
                        gumProjectSave, typeName);
                }
            }

        }

        public static void RemoveDuplicateVariables(this GumProjectSave gumProjectSave)
        {
            foreach (var component in gumProjectSave.Components)
            {
                RemoveDuplicateVariables(component);
            }
            foreach (var screen in gumProjectSave.Screens)
            {
                RemoveDuplicateVariables(screen);
            }
            foreach (var element in gumProjectSave.StandardElements)
            {
                RemoveDuplicateVariables(element);
            }


        }

        private static void RemoveDuplicateVariables(ElementSave element)
        {
            foreach (var state in element.States)
            {
                List<string> alreadyVisitedVariables = new List<string>();

                for (int i = 0; i < state.Variables.Count; i++)
                {
                    string variableName = state.Variables[i].Name;

                    if (alreadyVisitedVariables.Contains(variableName))
                    {
                        state.Variables.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        alreadyVisitedVariables.Add(variableName);
                    }
                }

            }
        }

        public static void FixStandardVariables(this GumProjectSave gumProjectSave)
        {
            foreach (var element in gumProjectSave.StandardElements)
            {
                var defaultState = StandardElementsManager.Self.GetDefaultStateFor(element.Name);

                foreach (var variable in defaultState.Variables)
                {
                    element.DefaultState.GetVariableSave(variable.Name).CanOnlyBeSetInDefaultState = variable.CanOnlyBeSetInDefaultState;
                }
            }


        }
    }
}
