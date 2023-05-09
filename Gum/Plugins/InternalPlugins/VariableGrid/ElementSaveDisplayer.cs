using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Gum.DataTypes.Variables;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.DataTypes.ComponentModel;
using Gum.Managers;
using Gum.PropertyGridHelpers.Converters;
using System.Drawing.Design;
using Gum.Plugins;
using Gum.Logic;
using System.Management.Instrumentation;
using WpfDataUi.DataTypes;
using Gum.Wireframe;
using Newtonsoft.Json.Linq;
using WpfDataUi.Controls;
using static System.Resources.ResXFileRef;
using GumRuntime;
using Svg;

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
            List<InstanceSavePropertyDescriptor> popertyList = new List<InstanceSavePropertyDescriptor>();

            if (instanceSave != null && stateSave != null)
            {
                DisplayCurrentInstance(popertyList, instanceSave);

            }
            else if (elementSave != null && stateSave != null)
            {
                StateSave defaultState = GetRecursiveStateFor(elementSave);

                DisplayCurrentElement(popertyList, elementSave, null, defaultState);


            }

            return popertyList;
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
                // early continue
                var browsableAttribute = propertyDescriptor.Attributes?.FirstOrDefault(item => item is BrowsableAttribute);

                var isMarkedAsNotBrowsable = browsableAttribute != null && (browsableAttribute as BrowsableAttribute).Browsable == false;
                if (isMarkedAsNotBrowsable)
                {
                    continue;
                }

                StateReferencingInstanceMember srim;

                if (instance != null)
                {
                    srim = new StateReferencingInstanceMember(propertyDescriptor, stateSave, stateSaveCategory, instance.Name + "." + propertyDescriptor.Name, instance, element);
                }
                else
                {
                    srim =
                        new StateReferencingInstanceMember(propertyDescriptor, stateSave, stateSaveCategory, propertyDescriptor.Name, instance, element);
                }

                srim.SetToDefault += (memberName) => ResetVariableToDefault(srim);
                srim.DetailText = propertyDescriptor.Subtext;
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

                    srim.SetToDefault += (memberName) => ResetVariableToDefault(srim);
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

        private void ResetVariableToDefault(StateReferencingInstanceMember srim)
        {
            string variableName = srim.Name;

            bool shouldReset = false;
            bool affectsTreeView = false;

            var selectedElement = SelectedState.Self.SelectedElement;
            var selectedInstance = SelectedState.Self.SelectedInstance;

            if (selectedInstance != null)
            {
                affectsTreeView = variableName == "Parent";
                //variableName = SelectedState.Self.SelectedInstance.Name + "." + variableName;

                shouldReset = true;
            }
            else if (selectedElement != null)
            {
                shouldReset =
                    // Don't let the user reset standard element variables, they have to have some actual value
                    (selectedElement is StandardElementSave) == false ||
                    // ... unless it's not the default
                    SelectedState.Self.SelectedStateSave != SelectedState.Self.SelectedElement.DefaultState;
            }

            // now we reset, but we don't remove the variable:
            //if(shouldReset)
            //{
            //    // If the variable is part of a category, then we don't allow setting the variable to default - they gotta do it through the cateory itself

            //    if (isPartOfCategory)
            //    {
            //        var window = new DeletingVariablesInCategoriesMessageBox();
            //        window.ShowDialog();

            //        shouldReset = false;
            //    }
            //}

            if (shouldReset)
            {
                bool isPartOfCategory = srim.StateSaveCategory != null;

                StateSave state = SelectedState.Self.SelectedStateSave;
                bool wasChangeMade = false;
                VariableSave variable = state.GetVariableSave(variableName);
                var oldValue = variable?.Value;
                if (variable != null)
                {
                    // Don't remove the variable if it's part of an element - we still want it there
                    // so it can be set, we just don't want it to set a value
                    // Update August 13, 2013
                    // Actually, we do want to remove it if it's part of an element but not the
                    // default state
                    // Update October 17, 2017
                    // Now that components do not
                    // necessarily need to have all
                    // of their variables, we can remove
                    // the variable now. In fact, we should
                    //bool shouldRemove = SelectedState.Self.SelectedInstance != null ||
                    //    SelectedState.Self.SelectedStateSave != SelectedState.Self.SelectedElement.DefaultState;
                    // Also, don't remove it if it's an exposed variable, this un-exposes things
                    bool shouldRemove = string.IsNullOrEmpty(variable.ExposedAsName) && !isPartOfCategory;

                    // Update October 7, 2019
                    // Actually, we can remove any variable so long as the current state isn't the "base definition" for it
                    // For elements - no variables are the base variable definitions except for variables that are categorized
                    // state variables for categories defined in this element
                    if (shouldRemove)
                    {
                        var isState = variable.IsState(selectedElement, out ElementSave categoryContainer, out StateSaveCategory categoryForVariable);

                        if (isState)
                        {
                            var isDefinedHere = categoryForVariable != null && categoryContainer == selectedElement;

                            shouldRemove = !isDefinedHere;
                        }
                    }


                    if (shouldRemove)
                    {
                        state.Variables.Remove(variable);
                    }
                    else if (isPartOfCategory)
                    {
                        var variableInDefault = SelectedState.Self.SelectedElement.DefaultState.GetVariableSave(variable.Name);
                        if (variableInDefault != null)
                        {
                            GumCommands.Self.GuiCommands.PrintOutput(
                                $"The variable {variable.Name} is part of the category {srim.StateSaveCategory.Name} so it cannot be removed. Instead, the value has been set to the value in the default state");

                            variable.Value = variableInDefault.Value;
                        }
                        else
                        {
                            GumCommands.Self.GuiCommands.PrintOutput("Could not set value to default because the default state doesn't set this value");

                        }

                    }
                    else
                    {
                        variable.Value = null;
                        variable.SetsValue = false;
                    }

                    wasChangeMade = true;
                    // We need to refresh the property grid and the wireframe display

                }
                else
                {
                    // Maybe this is a variable list?
                    VariableListSave variableList = state.GetVariableListSave(variableName);
                    if (variableList != null)
                    {
                        state.VariableLists.Remove(variableList);

                        // We don't support this yet:
                        // variableList.SetsValue = false; // just to be safe
                        wasChangeMade = true;
                    }
                }

                ElementSaveExtensions.ApplyVariableReferences(selectedElement, state);


                if (wasChangeMade)
                {
                    PropertyGridManager.Self.RefreshUI(force: true);
                    WireframeObjectManager.Self.RefreshAll(true);
                    SelectionManager.Self.Refresh();

                    PluginManager.Self.VariableSet(selectedElement, selectedInstance, variableName, oldValue);

                    if (affectsTreeView)
                    {
                        GumCommands.Self.GuiCommands.RefreshElementTreeView(SelectedState.Self.SelectedElement);
                    }

                    GumCommands.Self.FileCommands.TryAutoSaveElement(SelectedState.Self.SelectedElement);
                }
            }
            else
            {
                srim.IsDefault = false;
            }
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

                if(typeConverter is AvailableStatesConverter asAvailableStatesConverter)
                {
                    // This type converter is the standard one for this element type/category, but it's not instance-specific.
                    // We need it to be otherwise it pulls from CurrentInstance which is no good:
                    var copy = new AvailableStatesConverter(asAvailableStatesConverter.CategoryName);
                    if(instanceSave != null)
                    {
                        copy.InstanceSave = instanceSave;
                    }
                    typeConverter = copy;
                }

                property.TypeConverter = typeConverter;
                property.Category = category;
                property.Subtext = subtext;
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
