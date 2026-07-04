using System.Collections.Generic;
using System.Linq;
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
        /// <summary>
        /// Initializes a loaded project's elements (adding any missing default-state variables, parenting
        /// states, applying the Version&lt;1 SetsValue fixup, etc.), returning whether anything changed so
        /// the caller can decide to re-save.
        /// </summary>
        /// <param name="modifications">
        /// Optional. When provided, each element this call modifies appends a short reason (e.g.
        /// "Standard:Container", "Component:Button") so a caller can report <em>why</em> a freshly-loaded
        /// project was considered dirty. The bool return is unchanged; pass null (default) to ignore.
        /// </param>
        public static bool Initialize(this GumProjectSave gumProjectSave, bool tolerateMissingDefaultStates = false,
            ICollection<string>? modifications = null)
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
                    // Skip back-filling variables newer than the loaded project's version so an
                    // older project (e.g. a pre-v3 FRB1 project pinned to an older Gum runtime) is
                    // left byte-stable rather than injected with standard variables its generated
                    // runtime code can't compile against. See FlatRedBall issue #1881.
                    StateSave? effectiveDefaultState = FilterDefaultStateForProjectVersion(stateSave, gumProjectSave.Version);
                    // this will result in extra variables being
                    // added
                    List<string>? standardElementAdded = modifications != null ? new List<string>() : null;
                    if (standardElementSave.Initialize(effectiveDefaultState, modifications: standardElementAdded))
                    {
                        wasModified = true;
                        modifications?.Add($"Standard:{standardElementSave.Name}{FormatAddedVariables(standardElementAdded)}");
                    }

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
                List<string>? screenAdded = modifications != null ? new List<string>() : null;
                if (screenSave.Initialize(stateSave, tolerateMissingDefaultStates, modifications: screenAdded))
                {
                    wasModified = true;
                    modifications?.Add($"Screen:{screenSave.Name}{FormatAddedVariables(screenAdded)}");
                }
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

                List<string>? defaultStateAdded = modifications != null ? new List<string>() : null;
                if (componentSave.Initialize(new StateSave { Name = "Default" }, modifications: defaultStateAdded))
                {
                    wasModified = true;
                    modifications?.Add($"Component:{componentSave.Name} (default-state init{FormatAddedVariables(defaultStateAdded)})");
                }

                List<string>? componentBaseAdded = modifications != null ? new List<string>() : null;
                if (componentSave.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Component"), modifications: componentBaseAdded))
                {
                    wasModified = true;
                    modifications?.Add($"Component:{componentSave.Name} (Component-base init{FormatAddedVariables(componentBaseAdded)})");
                }
            }

            foreach (var behavior in gumProjectSave.Behaviors)
            {

                if (behavior.Initialize())
                {
                    wasModified = true;
                    modifications?.Add($"Behavior:{behavior.Name}");
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
                modifications?.Add("VersionUpgradeBelow1");
            }

            return wasModified;
        }

        // Formats the variable names a component-Initialize pass back-filled, for the load-modification
        // diagnostics (empty when nothing was added or when detail collection was not requested).
        private static string FormatAddedVariables(List<string>? added)
        {
            if (added == null || added.Count == 0)
            {
                return "";
            }
            return "; added: " + string.Join(", ", added);
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
            foreach(string typeName in StandardElementsManager.Self.SeedableStandardTypes)
            {
                if (!gumProjectSave.StandardElements.ContainsName(typeName))
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

        /// <summary>
        /// Returns the standard-element default state to use when back-filling a loaded element,
        /// with any variable whose <see cref="VariableSave.MinimumGumxVersion"/> exceeds
        /// <paramref name="projectVersion"/> removed. Variables default to MinimumGumxVersion 0, so
        /// pre-v3 variables and every non-shape standard pass through untouched (the original state
        /// is returned with no clone). Only when a gated variable is actually present does this clone
        /// and prune, so an older project is not injected with newer standard variables its (older,
        /// pinned) runtime can't compile. See FlatRedBall issue #1881.
        /// </summary>
        private static StateSave? FilterDefaultStateForProjectVersion(StateSave? defaultState, int projectVersion)
        {
            if (defaultState == null)
            {
                return null;
            }

            // Gather the gated names off the canonical (un-cloned) default first, then prune by
            // name — the version tag is read here, before any Clone, so the prune doesn't depend on
            // whether StateSave.Clone preserves the transient MinimumGumxVersion.
            HashSet<string> gatedVariableNames = defaultState.Variables
                .Where(variable => variable.MinimumGumxVersion > projectVersion)
                .Select(variable => variable.Name)
                .ToHashSet();

            if (gatedVariableNames.Count == 0)
            {
                return defaultState;
            }

            StateSave filtered = defaultState.Clone();
            filtered.Variables.RemoveAll(variable => gatedVariableNames.Contains(variable.Name));
            return filtered;
        }

        public static void FixStandardVariables(this GumProjectSave gumProjectSave)
        {
            foreach (var element in gumProjectSave.StandardElements)
            {
                var defaultState = StandardElementsManager.Self.GetDefaultStateFor(element.Name, throwExceptionOnMissing:false);
                if(defaultState != null)
                {
                    foreach (var variable in defaultState.Variables)
                    {
                        var variableInLoadedElement = element.DefaultState.GetVariableSave(variable.Name);

                        if (variableInLoadedElement == null)
                        {
                            // A version-gated variable that wasn't back-filled into this (older)
                            // project — there is nothing on the loaded element to reconcile. See
                            // FilterDefaultStateForProjectVersion / FRB #1881.
                            continue;
                        }

                        variableInLoadedElement.CanOnlyBeSetInDefaultState = variable.CanOnlyBeSetInDefaultState;
                        variableInLoadedElement.DesiredOrder = variable.DesiredOrder;
                    }
                }
            }

        }

        /// <summary>
        /// Issue #2947 — Circle used to expose a "Radius" variable. It now sizes via Width/Height
        /// like every other visual (the rendered radius is min(Width, Height)/2), so saved
        /// "Radius" values are converted to Width = Height = Radius * 2 to preserve existing sizes.
        /// </summary>
        /// <remarks>
        /// Only variables whose final name segment is exactly "Radius" are migrated, so
        /// GradientInnerRadius, GradientOuterRadius, and CornerRadius are left untouched. Handles
        /// both an unqualified "Radius" (a Circle-derived element overriding it in its own state)
        /// and a qualified "Instance.Radius" (a Circle instance inside another element).
        /// </remarks>
        public static bool MigrateCircleRadiusToWidthHeight(this GumProjectSave gumProjectSave)
        {
            bool didChange = false;
            foreach (var element in gumProjectSave.AllElements)
            {
                foreach (var state in element.AllStates)
                {
                    if (MigrateRadiusInState(state))
                    {
                        didChange = true;
                    }
                }
            }
            return didChange;
        }

        /// <summary>
        /// Issue #3009 — Circle and Rectangle no longer store a standalone gradient start color
        /// (Red1/Green1/Blue1/Alpha1 / Color1); the gradient start is now the active body color
        /// (FillColor when filled, StrokeColor otherwise). Strips any orphaned Red1/Green1/Blue1/
        /// Alpha1 variables left on Circle/Rectangle elements (and Circle/Rectangle-derived
        /// elements) and on their instances. Arc — which keeps Color1 as an obsolete back-compat
        /// shim — and the legacy RoundedRectangle/ColoredCircle shapes are left untouched.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="ObjectFinder.GetRootStandardElementSave(ElementSave?)"/> to resolve the
        /// root standard type, so it correctly targets only Circle/Rectangle (and derived) and skips
        /// Arc / legacy shapes. Color2 (Red2/Green2/Blue2/Alpha2 — the standalone second gradient
        /// stop) is preserved.
        /// </remarks>
        public static bool StripCircleRectangleGradientColor1(this GumProjectSave gumProjectSave)
        {
            bool didChange = false;
            foreach (var element in gumProjectSave.AllElements)
            {
                // Unqualified (element-level) channels are stripped when the element itself roots
                // at Circle/Rectangle (e.g. the standard Circle/Rectangle, or a component derived
                // from one overriding the value in its own state).
                bool elementIsCircleOrRectangle =
                    IsCircleOrRectangleRoot(ObjectFinder.Self.GetRootStandardElementSave(element));

                foreach (var state in element.AllStates)
                {
                    if (StripGradientColor1InState(state, element, elementIsCircleOrRectangle))
                    {
                        didChange = true;
                    }
                }
            }
            return didChange;
        }

        private static readonly string[] GradientColor1ChannelNames =
            new[] { "Red1", "Green1", "Blue1", "Alpha1" };

        private static bool IsCircleOrRectangleRoot(StandardElementSave? root)
        {
            return root != null && (root.Name == "Circle" || root.Name == "Rectangle");
        }

        private static bool StripGradientColor1InState(StateSave state, ElementSave element, bool elementIsCircleOrRectangle)
        {
            var toRemove = state.Variables
                .Where(v => v.Name != null && GradientColor1ChannelNames.Contains(GetLastNameSegment(v.Name)))
                .Where(v =>
                {
                    var sourceObject = v.SourceObject;
                    if (string.IsNullOrEmpty(sourceObject))
                    {
                        // Element-level channel: strip only when this element is a Circle/Rectangle.
                        return elementIsCircleOrRectangle;
                    }
                    // Instance-qualified channel: strip only when the instance roots at Circle/Rectangle.
                    var instance = element.Instances.FirstOrDefault(i => i.Name == sourceObject);
                    return instance != null
                        && IsCircleOrRectangleRoot(ObjectFinder.Self.GetRootStandardElementSave(instance));
                })
                .ToList();

            foreach (var variable in toRemove)
            {
                state.Variables.Remove(variable);
            }

            return toRemove.Count > 0;
        }

        private static bool MigrateRadiusInState(StateSave state)
        {
            const string radiusName = "Radius";

            // Snapshot first because Width/Height variables are added while iterating.
            var radiusVariables = state.Variables
                .Where(v => v.Name != null && GetLastNameSegment(v.Name) == radiusName && v.Value is float)
                .ToList();

            foreach (var radiusVariable in radiusVariables)
            {
                var prefix = radiusVariable.Name.Substring(0, radiusVariable.Name.Length - radiusName.Length);
                var diameter = (float)radiusVariable.Value * 2f;

                SetOrAddFloat(state, prefix + "Width", diameter);
                SetOrAddFloat(state, prefix + "Height", diameter);

                state.Variables.Remove(radiusVariable);
            }

            return radiusVariables.Count > 0;
        }

        private static void SetOrAddFloat(StateSave state, string variableName, float value)
        {
            var existing = state.Variables.FirstOrDefault(v => v.Name == variableName);
            if (existing != null)
            {
                existing.Type = "float";
                existing.Value = value;
                existing.SetsValue = true;
            }
            else
            {
                state.Variables.Add(new VariableSave { Name = variableName, Type = "float", Value = value, SetsValue = true });
            }
        }

        private static string GetLastNameSegment(string variableName)
        {
            var lastDotIndex = variableName.LastIndexOf('.');
            return lastDotIndex < 0 ? variableName : variableName.Substring(lastDotIndex + 1);
        }
    }
}
