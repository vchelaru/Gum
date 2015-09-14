﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Managers;
using Gum.DataTypes.Variables;

namespace Gum.DataTypes
{
    public static class GumProjectSaveExtensionMethods
    {
        /// <summary>
        /// Initializes the GumProjectSave for editing in Gum.  This means
        /// adding any variables that are necessary, fixing enumerations, and
        /// checking for other errors.
        /// </summary>
        /// <param name="gumProjectSave">The GumProjectSave</param>
        public static void Initialize(this GumProjectSave gumProjectSave)
        {
            gumProjectSave.ScreenReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
            gumProjectSave.ComponentReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
            gumProjectSave.StandardElementReferences.Sort((first, second) => first.Name.CompareTo(second.Name));

            gumProjectSave.Screens.Sort((first, second) => first.Name.CompareTo(second.Name));
            gumProjectSave.Components.Sort((first, second) => first.Name.CompareTo(second.Name));
            gumProjectSave.StandardElements.Sort((first, second) => first.Name.CompareTo(second.Name));


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

                stateSave.ParentContainer = standardElementSave;
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
                componentSave.Initialize(StandardElementsManager.Self.DefaultStates["Component"]);
            }

            if(gumProjectSave.Version < 1)
            {
                // This means that all default variables have SetValue = false
                // We need to fix that
                foreach (StandardElementSave standardElementSave in gumProjectSave.StandardElements)
                {
                    var defaultState = standardElementSave.DefaultState;

                    foreach(var variable in defaultState.Variables)
                    {
                        if(variable.IsState(standardElementSave) == false)
                        {
                            variable.SetsValue = true;
                        }
                    }
                }

                foreach(var component in gumProjectSave.Components)
                {
                    // We only want to do this on components that don't inherit from other components:
                    var baseComponent = ObjectFinder.Self.GetComponent(component.BaseType);

                    if (baseComponent == null)
                    {

                        var defaultState = component.DefaultState;


                        foreach (var variable in defaultState.Variables)
                        {
                            if (variable.IsState(component) == false)
                            {
                                variable.SetsValue = true;
                            }
                        }
                    }
                }
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
            foreach (var state in element.AllStates)
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
                    var variableInLoadedElement = element.DefaultState.GetVariableSave(variable.Name);

                    variableInLoadedElement.CanOnlyBeSetInDefaultState = variable.CanOnlyBeSetInDefaultState;
                    variableInLoadedElement.DesiredOrder = variable.DesiredOrder;
                }
            }

        }
    }
}
