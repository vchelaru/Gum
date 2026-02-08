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
using Gum.Undo;
using System.Security.Principal;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using System.Collections;
using Gum.Commands;
using Gum.Reflection;

namespace Gum.PropertyGridHelpers;

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
    private readonly SubtextLogic _subtextLogic;
    private readonly ISelectedState _selectedState;
    private readonly IUndoManager _undoManager;
    private readonly TypeManager _typeManager;
    private readonly VariableSaveLogic _variableSaveLogic;
    private readonly CategorySortAndColorLogic _categorySortAndColorLogic;

    #endregion

    public ElementSaveDisplayer(SubtextLogic subtextLogic,
        TypeManager typeManager,
        ISelectedState selectedState,
        IUndoManager undoManager)
    {
        _subtextLogic = subtextLogic;
        _selectedState = selectedState;
        _undoManager = undoManager;;
        _typeManager = typeManager;
        _variableSaveLogic = new VariableSaveLogic();
        _categorySortAndColorLogic = new CategorySortAndColorLogic();
    }

    private List<InstanceSavePropertyDescriptor> GetProperties(ElementSave instanceOwner, InstanceSave instanceSave, StateSave stateSave)
    {
        // search terms: display properties, display variables, show variables, variable display, variable displayer
        List<InstanceSavePropertyDescriptor> propertyList = new List<InstanceSavePropertyDescriptor>();

        if (instanceSave != null && stateSave != null)
        {
            FillPropertyList(propertyList, instanceSave, instanceOwner);

        }
        else if (instanceOwner != null && stateSave != null)
        {
            StateSave defaultState = GetRecursiveStateFor(instanceOwner);

            FillPropertyList(propertyList, instanceOwner, null, defaultState);


        }

        return propertyList;
    }

    private void FillPropertyList(List<InstanceSavePropertyDescriptor> pdc, ElementSave instanceOwner,
        InstanceSave instanceSave, StateSave defaultState, AmountToDisplay amountToDisplay = AmountToDisplay.AllVariables)
    {
        var currentState = _selectedState.SelectedStateSave;
        bool isDefault = currentState == _selectedState.SelectedElement.DefaultState;
        if (instanceSave?.DefinedByBase == true)
        {
            isDefault = false;
        }

        var effectiveElementSave = instanceSave == null ? instanceOwner : instanceSave.GetBaseElementSave();

        bool isCustomType = (effectiveElementSave is StandardElementSave) == false;
        if (isCustomType || instanceSave != null)
        {
            AddNameAndBaseTypeProperties(pdc, instanceOwner, instanceSave, isReadOnly: isDefault == false);
        }

        if (instanceSave != null)
        {
            mHelper.AddProperty(pdc, "Locked", typeof(bool)).IsReadOnly = !isDefault;
        }

        var shouldShowChildContainer = effectiveElementSave is ComponentSave && instanceSave == null;
        if (shouldShowChildContainer)
        {
            var defaultChildContainerProperty = mHelper.AddProperty(pdc, "DefaultChildContainer", typeof(string));
            defaultChildContainerProperty.IsReadOnly = !isDefault;
            defaultChildContainerProperty.TypeConverter = new AvailableInstancesConverter();
        }
        // we can't remove it here, because it might be added as a regular variable below...


        var variableListName = "VariableReferences";
        if (instanceSave != null)
        {
            variableListName = instanceSave.Name + "." + variableListName;
        }

        Dictionary<string, string> variablesSetThroughReference = GetVariablesSetThroughReferences(effectiveElementSave, currentState, variableListName);


        // if component
        if (instanceSave == null && effectiveElementSave as ComponentSave != null)
        {
            var defaultElementState = StandardElementsManager.Self.GetDefaultStateFor("Component");
            var variables = defaultElementState.Variables;
            foreach (var item in variables)
            {
                // Don't add states here, because they're handled below from this object's Default:
                if (item.IsState(effectiveElementSave) == false)
                {
                    string variableName = item.Name;
                    var isReadonly = false;
                    string subtext = null;
                    var isSetByReference = variablesSetThroughReference.ContainsKey(variableName);
                    if (variablesSetThroughReference.ContainsKey(variableName))
                    {
                        isReadonly = true;
                        subtext = variablesSetThroughReference[variableName];
                    }
                    var property = GetPropertyDescriptor(effectiveElementSave, instanceSave, amountToDisplay, item, isReadonly, subtext, pdc);

                    if(property != null)
                    {
                        property.IsAssignedByReference = isSetByReference;
                        pdc.Add(property);
                    }
                }
            }
        }
        // else if screen
        else if (instanceSave == null && effectiveElementSave as ScreenSave != null)
        {
            var defaultElementState = StandardElementsManager.Self.GetDefaultStateFor("Screen");
            var variables = defaultElementState.Variables;

            foreach (var item in variables)
            {
                // Shouldn't we check state?
                string variableName = item.Name;
                var isReadonly = false;
                string subtext = null;
                var isSetByReference = variablesSetThroughReference.ContainsKey(variableName);
                if (isSetByReference)
                {
                    isReadonly = true;
                    subtext = variablesSetThroughReference[variableName];
                }
                var property = GetPropertyDescriptor(effectiveElementSave, instanceSave, amountToDisplay, item, isReadonly, subtext, pdc);

                if (property != null)
                {
                    property.IsAssignedByReference = isSetByReference;
                    pdc.Add(property);
                }
            }
        }


        // now that variables have been added we can remove the default child container:
        if(!shouldShowChildContainer)
        {
            string variableName = "DefaultChildContainer";
            pdc.RemoveAll(item => item.Name == variableName);
        }

        #region Loop through all variables

        Dictionary<string, string> exposedVariables = new Dictionary<string, string>();
        if (instanceSave != null)
        {
            if (instanceOwner != null)
            {
                var exposedVariablesOnThisInstance = instanceOwner.DefaultState.Variables
                    .Where(item => !string.IsNullOrEmpty(item.ExposedAsName) && item.SourceObject == instanceSave.Name);
                foreach (var variable in exposedVariablesOnThisInstance)
                {
                    var definingVariable = effectiveElementSave.GetVariableFromThisOrBase(variable.GetRootName()) ??
                        ObjectFinder.Self.GetRootVariable(variable.Name, instanceOwner);
                    if (definingVariable != null)
                    {
                        exposedVariables.Add(definingVariable.Name, variable.ExposedAsName);
                    }
                }
            }
        }

        // We want to use the default state to get all possible
        // variables because the default state will always set all
        // variables.  We then look at the current state to get the
        // actual value
        for (int i = 0; i < defaultState.Variables.Count; i++)
        {
            VariableSave defaultVariable = defaultState.Variables[i];

            string variableName = defaultVariable.Name;
            var isReadonly = false;
            string? subtext = null;
            var isSetByReference = variablesSetThroughReference.ContainsKey(variableName);
            if (isSetByReference)
            {
                isReadonly = true;
                subtext += "=" + variablesSetThroughReference[variableName];
            }

            if (instanceSave != null)
            {

                if (exposedVariables.ContainsKey(defaultVariable.Name))
                {
                    if (!string.IsNullOrEmpty(subtext))
                    {
                        subtext += "\n";
                    }
                    subtext += "Exposed as " + exposedVariables[defaultVariable.Name];
                }
            }

            var shouldSkip = false;

            if(currentState != effectiveElementSave.DefaultState &&
                defaultVariable.IsState(effectiveElementSave, out ElementSave categoryContainer, out StateSaveCategory category))
            {
                if(category?.States.Contains(currentState) == true)
                {
                    shouldSkip = true;
                }
            }

            if(!shouldSkip)
            {
                var property = GetPropertyDescriptor(effectiveElementSave, instanceSave, amountToDisplay, defaultVariable, isReadonly, subtext, pdc);
                if(property != null)
                {
                    property.IsAssignedByReference = isSetByReference;
                    pdc.Add(property);
                }
            }
        }

        #endregion
    }

    private static Dictionary<string, string> GetVariablesSetThroughReferences(ElementSave elementSave, StateSave currentState, string variableListName)
    {
        Dictionary<string, string> variablesSetThroughReference = new Dictionary<string, string>();

        var value = currentState.GetValueRecursive(variableListName) as IList;
        if (value != null)
        {
            foreach (var item in value)
            {
                var assignment = item as string;
                if (assignment?.Contains("=") == true)
                {
                    var indexOfEquals = assignment.IndexOf("=");
                    var variableName = assignment.Substring(0, indexOfEquals).Trim();
                    var rightSideEquals = assignment.Substring(indexOfEquals + 1).Trim();
                    variablesSetThroughReference[variableName] = rightSideEquals;
                }
            }
        }

        HashSet<InstanceSave> instancesWithExposedVariables = new HashSet<InstanceSave>();
        foreach (var variable in elementSave.DefaultState.Variables)
        {
            if (!string.IsNullOrEmpty(variable.SourceObject) && !string.IsNullOrEmpty(variable.ExposedAsName))
            {
                var instance = elementSave.GetInstance(variable.SourceObject);
                if (instance != null)
                {
                    instancesWithExposedVariables.Add(instance);
                }
            }
        }

        foreach (var instanceWithExposedVariables in instancesWithExposedVariables)
        {
            var instanceVariableListName = "VariableReferences";

            var instanceRfv = new RecursiveVariableFinder(instanceWithExposedVariables, elementSave);

            var variable = instanceRfv.GetVariableList(instanceVariableListName);

            if (variable?.ValueAsIList != null)
            {
                foreach (string item in variable.ValueAsIList)
                {
                    if(item?.StartsWith("//") == true)
                    {
                        continue;
                    }
                    var indexOfEquals = item.IndexOf("=");

                    if(indexOfEquals == -1)
                    {
                        continue;
                    }

                    try
                    {
                        var variableName = instanceWithExposedVariables.Name + "." + item.Substring(0, indexOfEquals).Trim();
                        var rightSideEquals = item.Substring(indexOfEquals + 1).Trim();
                        variablesSetThroughReference[variableName] = rightSideEquals;
                    }
                    // swallow this exception, anything could be typed in this text box, and we don't
                    // want to crash here because of it.
                    catch { }
                }
            }
        }

        return variablesSetThroughReference;
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


    public List<MemberCategory> GetCategories(ElementSave instanceOwner, InstanceSave instance, List<MemberCategory> categories, StateSave stateSave, StateSaveCategory stateSaveCategory)
    {
        var properties = GetProperties(instanceOwner, instance, stateSave);

        StateSave defaultState;
        if(instance == null)
        {
            defaultState = GetRecursiveStateFor(instanceOwner);
        }
        else
        {
            GetDefaultState(instance, out ElementSave elementSave, out defaultState);
        }



        foreach (InstanceSavePropertyDescriptor propertyDescriptor in properties)
        {
            var srim = ToStateReferencingInstanceMember(instanceOwner, instance, stateSave, stateSaveCategory, propertyDescriptor);

            if(srim == null)
            {
                continue;
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

            bool shouldInclude = GetIfShouldInclude(variableList, instanceOwner, instance)
                && !variableList.IsHiddenInPropertyGrid;

            if (shouldInclude)
            {
                //Attribute[] customAttributes = GetAttributesForVariable(variableList);
                //Type type = typeof(List<string>);

                StateReferencingInstanceMember srim;

                Type? type = null;

                if(variableList.Type == "string")
                {
                    type = typeof(List<string>);
                }

                // todo - eventually move these up to a constructor. 
                var _editVariableService = Locator.GetRequiredService<IEditVariableService>();
                var _exposeVariableService = Locator.GetRequiredService<IExposeVariableService>();
                var _hotkeyManager = Locator.GetRequiredService<HotkeyManager>();
                var _deleteVariableService = Locator.GetRequiredService<IDeleteVariableService>();
                var _guiCommands = Locator.GetRequiredService<IGuiCommands>();
                var _fileCommands = Locator.GetRequiredService<IFileCommands>();
                var _setVariableLogic = Locator.GetRequiredService<ISetVariableLogic>();
                var _wireframeObjectManager = Locator.GetRequiredService<WireframeObjectManager>();

                var propertyDescriptor = new InstanceSavePropertyDescriptor(variableList.Name, type, null);
                if (instance != null)
                {
                    srim =
                    new StateReferencingInstanceMember(
                        propertyDescriptor, 
                        stateSave, 
                        stateSaveCategory, 
                        instance.Name + "." + propertyDescriptor.Name, 
                        instance, 
                        instanceOwner, 
                        _undoManager,
                        _editVariableService,
                        _exposeVariableService,
                        _hotkeyManager,
                        _deleteVariableService,
                        _selectedState,
                        _guiCommands,
                        _fileCommands,
                        _setVariableLogic,
                        _wireframeObjectManager);
                }
                else
                {
                    srim =
                        new StateReferencingInstanceMember(
                            propertyDescriptor, 
                            stateSave, 
                            stateSaveCategory, 
                            propertyDescriptor.Name, 
                            instance, 
                            instanceOwner, 
                            _undoManager,
                            _editVariableService,
                            _exposeVariableService,
                            _hotkeyManager,
                            _deleteVariableService,
                            _selectedState,
                            _guiCommands,
                            _fileCommands,
                            _setVariableLogic,
                            _wireframeObjectManager);
                }

                // moved to internal
                //srim.SetToDefault += (memberName) => ResetVariableToDefault(srim);
                // Only if it wasn't already set:
                if(srim.PreferredDisplayer == null)
                {
                    srim.PreferredDisplayer = typeof(ListBoxDisplay);
                }

                string? category = propertyDescriptor.Category?.Trim();

                var categoryToAddTo = categories.FirstOrDefault(item => item.Name == category);

                if (categoryToAddTo == null)
                {
                    categoryToAddTo = new MemberCategory(category);
                    categories.Add(categoryToAddTo);
                }

                categoryToAddTo.Members.Add(srim);
            }
        }

        categories = _categorySortAndColorLogic.SortAndColorCategories(categories);

        return categories;
    }



    private StateReferencingInstanceMember ToStateReferencingInstanceMember(ElementSave instanceOwner, InstanceSave instance, 
        StateSave stateSave, StateSaveCategory stateSaveCategory, InstanceSavePropertyDescriptor propertyDescriptor)
    {
        StateReferencingInstanceMember srim;

        // early continue
        var browsableAttribute = propertyDescriptor.Attributes?.FirstOrDefault(item => item is BrowsableAttribute);

        var isMarkedAsNotBrowsable = browsableAttribute != null && (browsableAttribute as BrowsableAttribute)?.Browsable == false;
        if (isMarkedAsNotBrowsable)
        {
            return null;
        }

        string variableName = "";
        if (instance != null)
        {
            variableName = instance.Name + "." + propertyDescriptor.Name;
        }
        else
        {
            variableName = propertyDescriptor.Name;
        }

        // todo - eventually move these up to a constructor. 
        var _editVariableService = Locator.GetRequiredService<IEditVariableService>();
        var _exposeVariableService = Locator.GetRequiredService<IExposeVariableService>();
        var _hotkeyManager = Locator.GetRequiredService<HotkeyManager>();
        var _deleteVariableService = Locator.GetRequiredService<IDeleteVariableService>();
        var _guiCommands = Locator.GetRequiredService<IGuiCommands>();
        var _fileCommands = Locator.GetRequiredService<IFileCommands>();
        var _setVariableLogic = Locator.GetRequiredService<ISetVariableLogic>();
        var _wireframeObjectManager = Locator.GetRequiredService<WireframeObjectManager>();

        srim = new StateReferencingInstanceMember(
            propertyDescriptor, 
            stateSave, 
            stateSaveCategory, 
            variableName, 
            instance, 
            instanceOwner, 
            _undoManager,
            _editVariableService,
            _exposeVariableService,
            _hotkeyManager,
            _deleteVariableService,
            _selectedState,
            _guiCommands,
            _fileCommands,
            _setVariableLogic,
            _wireframeObjectManager
            );

        // moved to internal
        //srim.SetToDefault += (memberName) => ResetVariableToDefault(srim);
        SetSubtext(stateSave, propertyDescriptor, srim, variableName);

        if(stateSave != null)
        {
            if (_subtextLogic.HasSubtextFunctionFor(stateSave, variableName))
            {
                srim.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == "Value")
                    {
                        SetSubtext(stateSave, propertyDescriptor, srim, variableName);
                    }
                };
            }
        }

        return srim;
    }

    private void SetSubtext(StateSave stateSave, InstanceSavePropertyDescriptor propertyDescriptor, StateReferencingInstanceMember srim, string variableName)
    {
        srim.DetailText = propertyDescriptor.Subtext;
        string? extraDetail = null;
        if (stateSave != null)
        {
            extraDetail = _subtextLogic.GetSubtextForCurrentState(stateSave, variableName);


        }
        if (!string.IsNullOrEmpty(extraDetail))
        {
            if (!string.IsNullOrEmpty(srim.DetailText))
            {
                srim.DetailText += "\n";
            }
            srim.DetailText += extraDetail;
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

        // https://github.com/vchelaru/Gum/issues/1023
        foreach (var category in elementSave.Categories)
        {
            var expectedName = category.Name + "State";

            var variable = elementSave.GetVariableFromThisOrBase(expectedName);
            if (variable != null)
            {
                stateToAddTo.Variables.Add(variable);
            }
        }

        return stateToAddTo;
    }

    private void FillPropertyList(List<InstanceSavePropertyDescriptor> properties, InstanceSave instanceSave, ElementSave instanceOwner)
    {
        ElementSave instanceBaseType;
        StateSave defaultStateForInstanceBaseTypeElement;
        GetDefaultState(instanceSave, out instanceBaseType, out defaultStateForInstanceBaseTypeElement);

        if(instanceBaseType != null)
        {
            FillPropertyList(properties, instanceOwner, instanceSave, defaultStateForInstanceBaseTypeElement, AmountToDisplay.ElementAndExposedOnly);
        }
        else
        {
            // We have an instance that has been selected that does not have a base type.
            // This can happen if the instance references a component type that doesn't exist.
            // We still want to let the user make edits to this object to fix the problem:
            var currentState = _selectedState.SelectedStateSave;
            bool isDefault = currentState == _selectedState.SelectedElement.DefaultState;
            if (instanceSave?.DefinedByBase == true)
            {
                isDefault = false;
            }

            if (instanceSave != null)
            {
                AddNameAndBaseTypeProperties(properties, instanceOwner, instanceSave, isReadOnly: isDefault == false);

            }
        }
    }

    /// <summary>
    /// Retrieves the base element type and its default state for the specified instance.
    /// </summary>
    /// <param name="instanceSave">The instance for which to obtain the base element type and default state. Cannot be null.</param>
    /// <param name="instanceBaseType">When this method returns, contains the base element type associated with the instance, or null if none is
    /// found.</param>
    /// <param name="defaultState">When this method returns, contains the default state for the base element type. If no base type is found,
    /// contains a new default state.</param>
    private static void GetDefaultState(InstanceSave instanceSave, out ElementSave instanceBaseType, out StateSave defaultState)
    {
        instanceBaseType = instanceSave.GetBaseElementSave();
        if (instanceBaseType != null)
        {
            if (instanceBaseType is StandardElementSave)
            {
                // if we use the standard elements manager, we don't get any custom categories, so we need to add those:
                defaultState = StandardElementsManager.Self.GetDefaultStateFor(instanceBaseType.Name).Clone();
                foreach (var category in instanceBaseType.Categories)
                {
                    var expectedName = category.Name + "State";

                    var variable = instanceBaseType.GetVariableFromThisOrBase(expectedName);
                    if (variable != null)
                    {
                        defaultState.Variables.Add(variable);
                    }
                }
            }
            else
            {
                defaultState = GetRecursiveStateFor(instanceBaseType);
            }
        }
        else
        {
            defaultState = new StateSave();
        }
    }

    private InstanceSavePropertyDescriptor GetPropertyDescriptor(ElementSave elementSave, InstanceSave instanceSave, 
        AmountToDisplay amountToDisplay, VariableSave defaultVariable, bool forceReadOnly, string subtext, List<InstanceSavePropertyDescriptor> existingItems)
    {
        ElementSave container = elementSave;
        if (instanceSave != null)
        {
            container = instanceSave.ParentContainer;
        }

        // Not sure why we were passing elementSave to this function:
        // I added a container object
        //bool shouldInclude = GetIfShouldInclude(defaultVariable, elementSave, instanceSave, ses);
        bool shouldInclude = _variableSaveLogic.GetIfVariableIsActive(defaultVariable, container, instanceSave);

        shouldInclude &= (
            string.IsNullOrEmpty(defaultVariable.SourceObject) || 
            amountToDisplay == AmountToDisplay.AllVariables || 
            !string.IsNullOrEmpty(defaultVariable.ExposedAsName));

        shouldInclude &= existingItems.Any(item => item.Name == defaultVariable.Name) == false;

        var isState = defaultVariable.IsState(elementSave, out ElementSave categoryContainer, out StateSaveCategory categorySave);

        if(isState && shouldInclude)
        {
            // uncategorized states are only default, so don't show it:
            shouldInclude = categorySave != null;
        }

        if (shouldInclude)
        {
            TypeConverter typeConverter = null;

            if (typeConverter == null)
            {
                typeConverter = defaultVariable.GetTypeConverter(elementSave);
            }

            Attribute[] customAttributes = GetAttributesForVariable(defaultVariable);

            string category = null;
            if (!string.IsNullOrEmpty(defaultVariable.Category))
            {
                category = defaultVariable.Category;
            }
            else if (!string.IsNullOrEmpty(defaultVariable.ExposedAsName))
            {
                var baseVariable = ObjectFinder.Self.GetRootVariable(defaultVariable.Name, elementSave);
                if(baseVariable != null)
                {
                    category = baseVariable.Category;
                }
                else
                {
                    category = "Exposed";
                }
            }
            else if (isState)
            {
                category = "States and Visibility";
            }

            if(defaultVariable.Type == null)
            {
                throw new Exception($"Could not find type for {defaultVariable}");
            }

            //Type type = typeof(string);
            Type type;
            
            if(!string.IsNullOrEmpty(defaultVariable.Type))
            {
                type = _typeManager.GetTypeFromString(defaultVariable.Type);
            }
            else
            {
                var rootVariable = ObjectFinder.Self.GetRootVariable(defaultVariable.Name, elementSave);

                type = _typeManager.GetTypeFromString(rootVariable.Type);
            }

                string name = defaultVariable.Name;

            if (!string.IsNullOrEmpty(defaultVariable.ExposedAsName))
            {
                name = defaultVariable.ExposedAsName;
            }

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

            _subtextLogic.GetDefaultSubtext(defaultVariable, subtext, property, elementSave, instanceSave);
            return property;
        }
        return null;
    }


    private static void AddNameAndBaseTypeProperties(List<InstanceSavePropertyDescriptor> pdc, ElementSave? instanceOwner, InstanceSave instance, bool isReadOnly)
    {
        var nameProperty = mHelper.AddProperty(
            pdc,
            "Name", 
            typeof(string), 
            TypeDescriptor.GetConverter(typeof(string)));

        nameProperty.IsReadOnly = isReadOnly;


        var isExcluded = false;

        if(instanceOwner != null)
        {
            RecursiveVariableFinder rfv = null;
            if (instance != null)
            {
                rfv = new RecursiveVariableFinder(instance, instanceOwner);
            }
            else
            {
                rfv = new RecursiveVariableFinder(instanceOwner.DefaultState);
            }
            // create a fake variable here to see if it's excluded:
            var fakeBaseTypeVariable = new VariableSave
            {
                Name = "BaseType",
            };

            isExcluded = PluginManager.Self.ShouldExclude(
                fakeBaseTypeVariable, rfv);
        }


        if (!isExcluded)
        {

            var baseTypeConverter = new AvailableBaseTypeConverter(instanceOwner, instance);
            // We may want to support Screens inheriting from other Screens in the future, but for now we won't allow it
            var baseTypeProperty = mHelper.AddProperty(pdc,
                "BaseType", typeof(string), baseTypeConverter);

            baseTypeProperty.IsReadOnly = isReadOnly;
        }
    }

    private bool GetIfShouldInclude(VariableListSave variableList, ElementSave container, InstanceSave currentInstance)
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

            toReturn = _variableSaveLogic.GetShouldIncludeBasedOnBaseType(variableList, container, rootElementSave);
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
