using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Gum.DataTypes.Variables;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.DataTypes.ComponentModel;
using Gum.Managers;
using Gum.PropertyGridHelpers.Converters;
using Gum.Plugins;
using Gum.Logic;
using WpfDataUi.DataTypes;
using Gum.Wireframe;
using WpfDataUi.Controls;
using GumRuntime;
using Gum.DataTypes.Behaviors;
using Newtonsoft.Json.Linq;

namespace Gum.PropertyGridHelpers
{
    #region AmountToDisplay Enum

    public enum AmountToDisplay
    {
        AllVariables,
        ElementAndExposedOnly
    }
    #endregion

    public class ElementSaveDisplayer
    {
        #region Fields
        static EditorAttribute mFileWindowAttribute = new EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor));

        static PropertyDescriptorHelper mHelper = new PropertyDescriptorHelper();

        #endregion

        private List<InstanceSavePropertyDescriptor> GetProperties(ElementSave elementSave, InstanceSave instanceSave, StateSave stateSave)
        {
            // search terms: display properties, display variables, show variables, variable display, variable displayer
            List<InstanceSavePropertyDescriptor> propertyList = new List<InstanceSavePropertyDescriptor>();

            if (instanceSave != null && stateSave != null)
            {
                DisplayCurrentInstance(propertyList, instanceSave);

            }
            else if (elementSave != null && stateSave != null)
            {
                StateSave defaultState = GetRecursiveStateFor(elementSave);

                DisplayCurrentElement(propertyList, elementSave, null, defaultState);


            }

            return propertyList;
        }

        private static void DisplayCurrentElement(List<InstanceSavePropertyDescriptor> pdc, ElementSave elementSave,
            InstanceSave instanceSave, StateSave defaultState, AmountToDisplay amountToDisplay = AmountToDisplay.AllVariables)
        {
            var currentState = SelectedState.Self.SelectedStateSave;
            bool isDefault = currentState == SelectedState.Self.SelectedElement.DefaultState;
            if (instanceSave?.DefinedByBase == true)
            {
                isDefault = false;
            }

            bool isCustomType = (elementSave is StandardElementSave) == false;
            if (isCustomType || instanceSave != null)
            {
                AddNameAndBaseTypeProperties(pdc, elementSave, instanceSave, isReadOnly: isDefault == false);
            }

            if (instanceSave != null)
            {
                mHelper.AddProperty(pdc, "Locked", typeof(bool)).IsReadOnly = !isDefault;
            }

            if(elementSave is ComponentSave && instanceSave == null)
            {
                var defaultChildContainerProperty = mHelper.AddProperty(pdc, nameof(ComponentSave.DefaultChildContainer), typeof(string));
                defaultChildContainerProperty.IsReadOnly = !isDefault;
                defaultChildContainerProperty.TypeConverter = new AvailableInstancesConverter();
            }

            var recursiveVariableFinder = new RecursiveVariableFinder(currentState);
            var variableListName = "VariableReferences";
            if (instanceSave != null)
            {
                variableListName = instanceSave.Name + "." + variableListName;
            }
            var variableReference = recursiveVariableFinder.GetVariableList(variableListName);
            Dictionary<string, string> variablesSetThroughReference = new Dictionary<string, string>();

            if(variableReference?.ValueAsIList != null)
            {
                foreach(var item in variableReference.ValueAsIList)
                {
                    var assignment = item as string;
                    if(assignment?.Contains("=") == true)
                    {
                        var indexOfEquals = assignment.IndexOf("=");
                        var variableName = assignment.Substring(0, indexOfEquals).Trim();
                        var rightSideEquals = assignment.Substring(indexOfEquals + 1).Trim();
                        variablesSetThroughReference[variableName] = rightSideEquals;
                    }
                }
            }


            // if component
            if (instanceSave == null && elementSave as ComponentSave != null)
            {
                var defaultElementState = StandardElementsManager.Self.GetDefaultStateFor("Component");
                var variables = defaultElementState.Variables;
                foreach (var item in variables)
                {
                    // Don't add states here, because they're handled below from this object's Default:
                    if (item.IsState(elementSave) == false)
                    {
                        string variableName = item.Name;
                        var isReadonly = false;
                        string subtext = null;
                        if(variablesSetThroughReference.ContainsKey(variableName))
                        {
                            isReadonly = true;
                            subtext = variablesSetThroughReference[variableName];
                        }
                        TryDisplayVariableSave(pdc, elementSave, instanceSave, amountToDisplay, item, isReadonly, subtext);
                    }
                }
            }
            // else if screen
            else if (instanceSave == null && elementSave as ScreenSave != null)
            {
                var screenDefaultState = StandardElementsManager.Self.GetDefaultStateFor("Screen");
                foreach (var item in screenDefaultState.Variables)
                {
                    string variableName = item.Name;
                    var isReadonly = false;
                    string subtext = null;
                    if (variablesSetThroughReference.ContainsKey(variableName))
                    {
                        isReadonly = true;
                        subtext = variablesSetThroughReference[variableName];
                    }
                    TryDisplayVariableSave(pdc, elementSave, instanceSave, amountToDisplay, item, isReadonly, subtext);
                }
            }





            #region Loop through all variables

            // We want to use the default state to get all possible
            // variables because the default state will always set all
            // variables.  We then look at the current state to get the
            // actual value
            for (int i = 0; i < defaultState.Variables.Count; i++)
            {
                VariableSave defaultVariable = defaultState.Variables[i];

                string variableName = defaultVariable.Name;
                var isReadonly = false;
                string subtext = null;
                if (variablesSetThroughReference.ContainsKey(variableName))
                {
                    isReadonly = true;
                    subtext = variablesSetThroughReference[variableName];
                }
                TryDisplayVariableSave(pdc, elementSave, instanceSave, amountToDisplay, defaultVariable, isReadonly, subtext);
            }

            #endregion
        }

        public void GetCategories(BehaviorSave behavior, InstanceSave instance, List<MemberCategory> categories)
        {
            if (instance != null)
            {
                var propertyLists = new List<InstanceSavePropertyDescriptor>();
                AddNameAndBaseTypeProperties(propertyLists, null, instance, isReadOnly: false);

                foreach(var item in propertyLists)
                {
                    var srim = ToStateReferencingInstanceMember(null, instance, null, null, item);

                    if (srim == null)
                    {
                        continue;
                    }
                    string category = item.Category?.Trim();

                    var categoryToAddTo = categories.FirstOrDefault(item => item.Name == category);

                    if (categoryToAddTo == null)
                    {
                        categoryToAddTo = new MemberCategory(category);
                        categories.Add(categoryToAddTo);
                    }

                    categoryToAddTo.Members.Add(srim);
                }

            }

        }


        public void GetCategories(ElementSave element, InstanceSave instance, List<MemberCategory> categories, StateSave stateSave, StateSaveCategory stateSaveCategory)
        {
            var properties = GetProperties(element, instance, stateSave);

            StateSave defaultState;
            if(instance == null)
            {
                defaultState = GetRecursiveStateFor(element);
            }
            else
            {
                GetDefaultState(instance, out ElementSave elementSave, out defaultState);
            }



            foreach (InstanceSavePropertyDescriptor propertyDescriptor in properties)
            {
                var srim = ToStateReferencingInstanceMember(element, instance, stateSave, stateSaveCategory, propertyDescriptor);

                if(srim == null)
                {
                    return;
                }
                string category = propertyDescriptor.Category?.Trim();

                var categoryToAddTo = categories.FirstOrDefault(item => item.Name == category);

                if (categoryToAddTo == null)
                {
                    categoryToAddTo = new MemberCategory(category);
                    categories.Add(categoryToAddTo);
                }

                categoryToAddTo.Members.Add(srim);

            }

            // do variable lists last:
            for (int i = 0; i < defaultState.VariableLists.Count; i++)
            {
                VariableListSave variableList = defaultState.VariableLists[i];

                bool shouldInclude = GetIfShouldInclude(variableList, element, instance)
                    && !variableList.IsHiddenInPropertyGrid;

                if (shouldInclude)
                {
                    //Attribute[] customAttributes = GetAttributesForVariable(variableList);
                    //Type type = typeof(List<string>);

                    StateReferencingInstanceMember srim;
                    var propertyDescriptor = new InstanceSavePropertyDescriptor(variableList.Name, null, null);
                    if (instance != null)
                    {
                        srim =
                        new StateReferencingInstanceMember(propertyDescriptor, stateSave, stateSaveCategory, instance.Name + "." + propertyDescriptor.Name, instance, element);
                    }
                    else
                    {
                        srim =
                            new StateReferencingInstanceMember(propertyDescriptor, stateSave, stateSaveCategory, propertyDescriptor.Name, instance, element);
                    }

                    // moved to internal
                    //srim.SetToDefault += (memberName) => ResetVariableToDefault(srim);
                    srim.PreferredDisplayer = typeof(ListBoxDisplay);

                    string category = propertyDescriptor.Category?.Trim();

                    var categoryToAddTo = categories.FirstOrDefault(item => item.Name == category);

                    if (categoryToAddTo == null)
                    {
                        categoryToAddTo = new MemberCategory(category);
                        categories.Add(categoryToAddTo);
                    }

                    categoryToAddTo.Members.Add(srim);
                }
            }
        }

        private static StateReferencingInstanceMember ToStateReferencingInstanceMember(ElementSave element, InstanceSave instance, StateSave stateSave, StateSaveCategory stateSaveCategory, InstanceSavePropertyDescriptor propertyDescriptor)
        {
            StateReferencingInstanceMember srim;

            // early continue
            var browsableAttribute = propertyDescriptor.Attributes?.FirstOrDefault(item => item is BrowsableAttribute);

            var isMarkedAsNotBrowsable = browsableAttribute != null && (browsableAttribute as BrowsableAttribute).Browsable == false;
            if (isMarkedAsNotBrowsable)
            {
                return null;
            }


            if (instance != null)
            {
                srim = new StateReferencingInstanceMember(propertyDescriptor, stateSave, stateSaveCategory, instance.Name + "." + propertyDescriptor.Name, instance, element);
            }
            else
            {
                srim =
                    new StateReferencingInstanceMember(propertyDescriptor, stateSave, stateSaveCategory, propertyDescriptor.Name, instance, element);
            }

            // moved to internal
            //srim.SetToDefault += (memberName) => ResetVariableToDefault(srim);
            srim.DetailText = propertyDescriptor.Subtext;
            return srim;
        }

        private static StateSave GetRecursiveStateFor(ElementSave elementSave, StateSave stateToAddTo = null)
        {
            // go bottom up
            var baseElement = ObjectFinder.Self.GetElementSave(elementSave.BaseType);
            if (baseElement != null)
            {
                stateToAddTo = GetRecursiveStateFor(baseElement, stateToAddTo);
            }
            if(stateToAddTo == null)
            {
                stateToAddTo = new StateSave();
            }


            var existingVariableNames = stateToAddTo.Variables.Select(item => item.Name);
            var existingVariableListNames = stateToAddTo.VariableLists.Select(item => item.Name);

            if (elementSave is StandardElementSave)
            {
                var defaultStates = StandardElementsManager.Self.GetDefaultStateFor(elementSave.Name);
                var variablesToAdd = defaultStates.Variables
                    .Select(item => item.Clone())
                    .Where(item => existingVariableNames.Contains(item.Name) == false);

                stateToAddTo.Variables.AddRange(variablesToAdd);

                var variableListsToAdd = defaultStates.VariableLists
                    .Select(item => item.Clone())
                    .Where(item => existingVariableListNames.Contains(item.Name) == false);

                stateToAddTo.VariableLists.AddRange(variableListsToAdd);
            }
            else
            {
                var variablesToAdd = elementSave.DefaultState.Variables
                    .Select(item => item.Clone())
                    .Where(item => existingVariableNames.Contains(item.Name) == false);

                stateToAddTo.Variables.AddRange(variablesToAdd);

                var variableListsToAdd = elementSave.DefaultState.VariableLists
                    .Select(item => item.Clone())
                    .Where(item => existingVariableListNames.Contains(item.Name) == false);

                stateToAddTo.VariableLists.AddRange(variableListsToAdd);
            }



            return stateToAddTo;
        }

        private void DisplayCurrentInstance(List<InstanceSavePropertyDescriptor> pdc, InstanceSave instanceSave)
        {
            ElementSave elementSave;
            StateSave defaultState;
            GetDefaultState(instanceSave, out elementSave, out defaultState);

            DisplayCurrentElement(pdc, elementSave, instanceSave, defaultState, AmountToDisplay.ElementAndExposedOnly);

        }

        private static void GetDefaultState(InstanceSave instanceSave, out ElementSave elementSave, out StateSave defaultState)
        {
            elementSave = instanceSave.GetBaseElementSave();
            if (elementSave != null)
            {
                if (elementSave is StandardElementSave)
                {
                    // if we use the standard elements manager, we don't get any custom categories, so we need to add those:
                    defaultState = StandardElementsManager.Self.GetDefaultStateFor(elementSave.Name).Clone();
                    foreach (var category in elementSave.Categories)
                    {
                        var expectedName = category.Name + "State";

                        var variable = elementSave.GetVariableFromThisOrBase(expectedName);
                        if (variable != null)
                        {
                            defaultState.Variables.Add(variable);
                        }
                    }
                }
                else
                {
                    defaultState = GetRecursiveStateFor(elementSave);
                }
            }
            else
            {
                defaultState = new StateSave();
            }
        }

        private static void TryDisplayVariableSave(List<InstanceSavePropertyDescriptor> pdc, ElementSave elementSave, InstanceSave instanceSave, 
            AmountToDisplay amountToDisplay, VariableSave defaultVariable, bool forceReadOnly, string subtext)
        {
            ElementSave container = elementSave;
            if (instanceSave != null)
            {
                container = instanceSave.ParentContainer;
            }

            // Not sure why we were passing elementSave to this function:
            // I added a container object
            //bool shouldInclude = GetIfShouldInclude(defaultVariable, elementSave, instanceSave, ses);
            bool shouldInclude = Gum.Logic.VariableSaveLogic.GetIfVariableIsActive(defaultVariable, container, instanceSave);

            shouldInclude &= (
                string.IsNullOrEmpty(defaultVariable.SourceObject) || 
                amountToDisplay == AmountToDisplay.AllVariables || 
                !string.IsNullOrEmpty(defaultVariable.ExposedAsName));

            shouldInclude &= pdc.Any(item => item.Name == defaultVariable.Name) == false;

            var isState = defaultVariable.IsState(elementSave, out ElementSave categoryContainer, out StateSaveCategory categorySave);

            if(isState && shouldInclude)
            {
                // uncategorized states are only default, so don't show it:
                shouldInclude = categorySave != null;
            }

            if (shouldInclude)
            {
                TypeConverter typeConverter = defaultVariable.GetTypeConverter(elementSave);

                Attribute[] customAttributes = GetAttributesForVariable(defaultVariable);

                string category = null;
                if (!string.IsNullOrEmpty(defaultVariable.Category))
                {
                    category = defaultVariable.Category;
                }
                else if (!string.IsNullOrEmpty(defaultVariable.ExposedAsName))
                {
                    category = "Exposed";
                }
                else if(isState)
                {
                    category = "States and Visibility";
                }

                //Type type = typeof(string);
                Type type = Gum.Reflection.TypeManager.Self.GetTypeFromString(defaultVariable.Type);

                string name = defaultVariable.Name;

                if (!string.IsNullOrEmpty(defaultVariable.ExposedAsName))
                {
                    name = defaultVariable.ExposedAsName;
                }

                // if it already contains, do nothing
                var alreadyContains = pdc.Any(item => item.Name == name);

                InstanceSavePropertyDescriptor property = new InstanceSavePropertyDescriptor(name, type, customAttributes);

                property.IsReadOnly = forceReadOnly;

                // I think this is old code that screws up dropdowns because GetTypeConverter handles the proper assignment.
                //if(typeConverter is AvailableStatesConverter asAvailableStatesConverter &&
                //    // If the instance save is null, then the GetTypeConverter method above gets the right element/instance based on the
                //    // variable's SourceObject property.
                //    instanceSave != null)
                //{
                //    // This type converter is the standard one for this element type/category, but it's not instance-specific.
                //    // We need it to be otherwise it pulls from CurrentInstance which is no good:
                //    var copy = new AvailableStatesConverter(asAvailableStatesConverter.CategoryName);
                //    if(instanceSave != null)
                //    {
                //        copy.InstanceSave = instanceSave;
                //    }
                //    typeConverter = copy;
                //}

                property.TypeConverter = typeConverter;
                property.Category = category;


                property.Subtext = subtext;
                if(!string.IsNullOrEmpty(defaultVariable?.DetailText))
                {
                    property.Subtext += "\n" + defaultVariable.DetailText;
                }
                pdc.Add(property);
            }
        }

        private static void AddNameAndBaseTypeProperties(List<InstanceSavePropertyDescriptor> pdc, ElementSave elementSave, InstanceSave instance, bool isReadOnly)
        {
            var nameProperty = mHelper.AddProperty(
                pdc,
                "Name", 
                typeof(string), 
                TypeDescriptor.GetConverter(typeof(string)));

            nameProperty.IsReadOnly = isReadOnly;


            var baseTypeConverter = new AvailableBaseTypeConverter(elementSave, instance);

                // We may want to support Screens inheriting from other Screens in the future, but for now we won't allow it
            var baseTypeProperty = mHelper.AddProperty(pdc,
                "Base Type", typeof(string), baseTypeConverter);

            baseTypeProperty.IsReadOnly = isReadOnly;
        }

        private static bool GetIfShouldInclude(VariableListSave variableList, ElementSave container, InstanceSave currentInstance)
        {
            bool toReturn = (string.IsNullOrEmpty(variableList.SourceObject));

            if (toReturn)
            {
                StandardElementSave rootElementSave = null;

                if (currentInstance != null)
                {
                    rootElementSave = ObjectFinder.Self.GetRootStandardElementSave(currentInstance);
                }
                else if ((container is ScreenSave) == false)
                {
                    rootElementSave = ObjectFinder.Self.GetRootStandardElementSave(container);
                }

                toReturn = VariableSaveLogic.GetShouldIncludeBasedOnBaseType(variableList, container, rootElementSave);
            }

            return toReturn;
        }
            
        private static Attribute[] GetAttributesForVariable(VariableSave defaultVariable)
        {
            List<Attribute> attributes = new List<Attribute>();

            if (defaultVariable.IsFile)
            {
                attributes.Add(mFileWindowAttribute);
            }

            if (defaultVariable.IsHiddenInPropertyGrid)
            {
                attributes.Add(new BrowsableAttribute(false));
            }

            List<Attribute> attributesFromPlugins = PluginManager.Self.GetAttributesFor(defaultVariable);

            if(attributesFromPlugins.Count != 0)
            {
                attributes.AddRange(attributesFromPlugins);
            }

            return attributes.ToArray();
        }

    }
}
