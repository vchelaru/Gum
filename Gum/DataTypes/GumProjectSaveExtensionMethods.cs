using System.Collections.Generic;
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
        /// <param name="tolerateMissingDefaultStates">Whether to tolerate missing default states. If false, 
        /// exceptions are thrown if there is a missing standard state. If true, missing states will not throw an exception.</param>
        public static bool Initialize(this GumProjectSave gumProjectSave, bool tolerateMissingDefaultStates = false)
        {
            bool wasModified = false;

            SortElementAndBehaviors(gumProjectSave);

            // Do StandardElements first
            // because the values here are
            // used by components to set their
            // ignored enum values.
            foreach (StandardElementSave standardElementSave in gumProjectSave.StandardElements)
            {
                try
                {
                    StateSave? stateSave = StandardElementsManager.Self.GetDefaultStateFor(standardElementSave.Name);
                    // this will result in extra variables being
                    // added
                    wasModified = standardElementSave.Initialize(stateSave) || wasModified;

                    if(stateSave != null)
                    {
                        stateSave.ParentContainer = standardElementSave;
                    }

                }
                catch
                {
                    if (!tolerateMissingDefaultStates)
                    {
                        throw;
                    }
                    else
                    {
                        // we tolerate them, we want to make sure they have a parent container at least:
                        if(standardElementSave.DefaultState != null && 
                            standardElementSave.DefaultState.ParentContainer == null)
                        {
                            standardElementSave.DefaultState.ParentContainer = standardElementSave;
                        }
                    }
                }
            }

            foreach (ScreenSave screenSave in gumProjectSave.Screens)
            {
                var stateSave = StandardElementsManager.Self.GetDefaultStateFor("Screen");
                wasModified = screenSave.Initialize(stateSave) || wasModified;
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

                // October 17, 2017
                // We used to pass in
                // the base StandardElementSave
                // to copy the variables to the Component.
                // This is redundant. It adds data, makes things
                // slower...I just don't think we need it. It's especially
                // bad at runtime in games that are redundantly setting variables
                // which may result in reflection.
                //StateSave defaultStateSave = null;
                //StandardElementSave ses = ObjectFinder.Self.GetRootStandardElementSave(componentSave);
                //if (ses != null)
                //{
                //    defaultStateSave = ses.DefaultState;
                //}

                if (componentSave.Initialize(new StateSave { Name = "Default" }))
                {
                    wasModified = true;
                }

                if (componentSave.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Component")))
                {
                    wasModified = true;
                }
            }

            foreach (var behavior in gumProjectSave.Behaviors)
            {

                if (behavior.Initialize())
                {
                    wasModified = true;
                }
            }

            if (gumProjectSave.Version < 1)
            {
                // This means that all default variables have SetValue = false
                // We need to fix that
                foreach (StandardElementSave standardElementSave in gumProjectSave.StandardElements)
                {
                    var defaultState = standardElementSave.DefaultState;

                    foreach (var variable in defaultState.Variables)
                    {
                        if (variable.IsState(standardElementSave) == false)
                        {
                            variable.SetsValue = true;
                        }
                    }
                }

                foreach (var component in gumProjectSave.Components)
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
                gumProjectSave.Version = 1;
                wasModified = true;
            }

            return wasModified;
        }

        public static void SortElementAndBehaviors(this GumProjectSave gumProjectSave)
        {
            gumProjectSave.ScreenReferences?.Sort((first, second) => first.Name.CompareTo(second.Name));
            gumProjectSave.ComponentReferences?.Sort((first, second) => first.Name.CompareTo(second.Name));
            gumProjectSave.StandardElementReferences?.Sort((first, second) => first.Name.CompareTo(second.Name));
            gumProjectSave.BehaviorReferences?.Sort((first, second) =>
            {
                if (first?.Name == null)
                {
                    return 0;
                }
                else if (second?.Name == null)
                {
                    return 0;
                }
                else
                {
                    return first?.Name.CompareTo(second?.Name) ?? 0;
                }
            });

            gumProjectSave.Screens.Sort((first, second) => first.Name.CompareTo(second.Name));
            gumProjectSave.Components.Sort((first, second) => first.Name.CompareTo(second.Name));
            gumProjectSave.StandardElements.Sort((first, second) => first.Name.CompareTo(second.Name));
            gumProjectSave.Behaviors.Sort((first, second) => first.Name?.CompareTo(second.Name) ?? 0);
        }

        /// <summary>
        /// Adds any Standard Elements that have been created since the project was last saved.  This should be called
        /// when the project is first loaded.
        /// </summary>
        /// <param name="gumProjectSave">The gum project to add to</param>
        public static bool AddNewStandardElementTypes(this GumProjectSave gumProjectSave)
        {
            bool modified = false;
            foreach(string typeName in StandardElementsManager.Self.DefaultTypes)
            {
                if (typeName != "Screen" && !gumProjectSave.StandardElements.ContainsName(typeName))
                {
                    StandardElementsManager.Self.AddStandardElementSaveInstance(
                        gumProjectSave, typeName);
                    modified = true;
                }
            }
            return modified;
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
            foreach(var behavior in gumProjectSave.Behaviors)
            {
                foreach(var state in behavior.AllStates)
                {
                    RemoveDuplicateVariables(state);
                }
            }

        }

        private static void RemoveDuplicateVariables(ElementSave element)
        {
            foreach (var state in element.AllStates)
            {
                RemoveDuplicateVariables(state);

            }
        }

        private static void RemoveDuplicateVariables(StateSave state)
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

        public static void FixStandardVariables(this GumProjectSave gumProjectSave)
        {
            foreach (var element in gumProjectSave.StandardElements)
            {
                var defaultState = StandardElementsManager.Self.GetDefaultStateFor(element.Name);
                if(defaultState != null)
                {
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
}
