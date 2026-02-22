using Gum.Commands;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.RenderingLibrary;
using Gum.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ToolsUtilities;

namespace Gum.ToolCommands;

public class ElementCommands : IElementCommands
{
    #region Fields

    private readonly ISelectedState _selectedState;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IVariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly PluginManager _pluginManager;
    private readonly IProjectManager _projectManager;
    private readonly IProjectState _projectState;

    #endregion

    public ElementCommands(ISelectedState selectedState,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        IWireframeObjectManager wireframeObjectManager,
        PluginManager pluginManager,
        IProjectManager projectManager,
        IProjectState projectState)
    {
        _selectedState = selectedState;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _variableInCategoryPropagationLogic = variableInCategoryPropagationLogic;
        _wireframeObjectManager = wireframeObjectManager;
        _pluginManager = pluginManager;
        _projectManager = projectManager;
        _projectState = projectState;
    }

    #region Instance

    public InstanceSave AddInstance(ElementSave elementToAddTo, string name, string? type = null, string? parentName = null, int? desiredIndex = null)
    {
        InstanceSave instanceSave = new InstanceSave();
        instanceSave.Name = name;
        instanceSave.ParentContainer = elementToAddTo;
        instanceSave.BaseType = type ?? StandardElementsManager.Self.DefaultType;

        return AddInstance(elementToAddTo, instanceSave, parentName, desiredIndex);
    }

    public InstanceSave? AddInstance(ElementSave elementToAddTo, InstanceSave instanceSave, string? parentName = null, int? desiredIndex = null)
    {
        if (elementToAddTo == null)
        {
            throw new Exception("Could not add instance named " + instanceSave.Name + " because no element is selected");
        }

        if(desiredIndex == null)
        {
            elementToAddTo.Instances.Add(instanceSave);
        }
        else
        {
            elementToAddTo.Instances.Insert(desiredIndex.Value, instanceSave);
        }

        _guiCommands.RefreshElementTreeView(elementToAddTo);

        _wireframeObjectManager.RefreshAll(true);
        //_selectedState.SelectedInstance = instanceSave;

        // Set the parent before adding the instance in case plugins want to reject the creation of the object...
        if (!string.IsNullOrEmpty(parentName))
        {
            elementToAddTo.DefaultState.SetValue($"{instanceSave.Name}.Parent", parentName, "string");
        }

        // We need to call InstanceAdd before we select the new object - the Undo manager expects it
        _pluginManager.InstanceAdd(elementToAddTo, instanceSave);

        // a plugin may have removed this instance. If so, we need to refresh the tree node again:
        if (elementToAddTo.Instances.Contains(instanceSave) == false)
        {
            _guiCommands.RefreshElementTreeView(elementToAddTo);
            _wireframeObjectManager.RefreshAll(true);

            // August 2, 2022 - this is currently returned even if a plugin
            // removes the new instance. Should it be? Will it causes NullReferenceExceptions
            // on systems which always expect this to be non-null? Unsure....
            // August 4, 2022 - nope, this already is causing problems, we should return null.
            instanceSave = null;
        }
        else
        {
            _selectedState.SelectedInstance = instanceSave;
        }

        _fileCommands.TryAutoSaveElement(elementToAddTo);

        return instanceSave;
    }

    public string GetUniqueNameForNewInstance(ElementSave elementSaveForNewInstance, ElementSave containerForNewInstance)
    {
#if DEBUG
        if (elementSaveForNewInstance == null)
        {
            throw new ArgumentNullException("elementSave");
        }
#endif
        // remove the path - we dont want folders to be part of the name
        string name = FileManager.RemovePath(elementSaveForNewInstance.Name) + "Instance";
        IEnumerable<string> existingNames = containerForNewInstance.Instances.Select(i => i.Name);

        return StringFunctions.MakeStringUnique(name, existingNames);
    }

    #endregion

    #region State


    public StateSave AddState(IStateContainer stateContainer, StateSaveCategory category, string name)
    {
        // elementToAddTo may be null if category is not null
        if (stateContainer == null && category == null)
        {
            throw new Exception("Could not add state named " + name + " because no element is selected");
        }

        StateSave stateSave = new StateSave();
        stateSave.Name = name;
        AddState(stateContainer!, category, stateSave);

        return stateSave;
    }

    public void AddState(IStateContainer stateContainer, StateSaveCategory category, StateSave stateSave, int? desiredIndex = null)
    {
        AddStateInternal(stateContainer, category, stateSave);

        var otherState = category?.States.FirstOrDefault(item => item != stateSave);
        if (otherState != null && stateContainer is ElementSave elementSave)
        {
            foreach (var variable in otherState.Variables)
            {
                _variableInCategoryPropagationLogic
                    .PropagateVariablesInCategory(variable.Name, elementSave, category);
            }
        }


        _pluginManager.StateAdd(stateSave);

        _guiCommands.RefreshStateTreeView();

        if(stateContainer is BehaviorSave behavior)
        {
            _fileCommands.TryAutoSaveBehavior(behavior);
        }
        else
        {
            _fileCommands.TryAutoSaveElement(stateContainer as ElementSave);
        }
    }

    private void AddStateInternal(IStateContainer stateContainer, StateSaveCategory category, StateSave stateSave, int? desiredIndex = null)
    {
        stateSave.ParentContainer = stateContainer as ElementSave;

        if (category == null)
        {
            if(desiredIndex != null)
            {
                stateContainer.UncategorizedStates.Insert(desiredIndex.Value, stateSave);
            }
            else
            {
                stateContainer.UncategorizedStates.Add(stateSave);
            }
        }
        else
        {
            if (desiredIndex != null)
            {
                category.States.Insert(desiredIndex.Value, stateSave);
            }
            else
            {
                category.States.Add(stateSave);
            }
        }
    }

    #endregion

    #region Variables

    public void SortVariables()
    {
        var gumProject = _projectState.GumProjectSave;

        foreach (var elementSave in gumProject.AllElements)
        {
            SortVariables(elementSave);
        }
        foreach (var behavior in gumProject.Behaviors)
        {
            SortVariables(behavior);
        }
    }

    public void SortVariables(IStateContainer container)
    {
        foreach (var stateSave in container.AllStates)
        {
            stateSave.Variables.Sort((first, second) => first.Name.CompareTo(second.Name));
        }
    }

    public bool MoveSelectedObjectsBy(float xToMoveBy, float yToMoveBy)
    {
        bool hasChangeOccurred = false;
        // This can get called either by
        // click+drag or by nudge (and who
        // knows, maybe other parts of the code
        // in the future), so we should make sure
        // that something is really selected.
        var hasSelection = _selectedState.SelectedComponent != null ||
            _selectedState.SelectedStandardElement != null ||
            _selectedState.SelectedInstance != null;

        /////////////////////////Early out////////////////////////
        if(!hasSelection)
        {
            return hasChangeOccurred;
        }
        ///////////////////////End Early Out//////////////////////

        var isMovingElement = _selectedState.SelectedInstances.Count() == 0 &&
            (_selectedState.SelectedComponent != null || _selectedState.SelectedStandardElement != null);

        if (isMovingElement)
        {
            var element = _selectedState.SelectedElement;
            if (xToMoveBy != 0)
            {
                hasChangeOccurred = true;
                ModifyVariable("X", xToMoveBy, element);
            }
            if (yToMoveBy != 0)
            {
                hasChangeOccurred = true;
                ModifyVariable("Y", yToMoveBy, element);
            }
        }
        else
        {
            var selectedInstances = _selectedState.SelectedInstances;

            foreach (InstanceSave instance in selectedInstances)
            {
                bool shouldSkip = instance.Locked || ShouldSkipDraggingMovementOn(instance);

                if (!shouldSkip)
                {
                    // This could prevent a double-layout by locking layout until all values have been set
                    if (xToMoveBy != 0)
                    {
                        hasChangeOccurred = true;
                        float value = ModifyVariable("X", xToMoveBy, instance);
                    }
                    if (yToMoveBy != 0)
                    {
                        hasChangeOccurred = true;
                        float value = ModifyVariable("Y", yToMoveBy, instance);
                    }
                }
            }
        }

        if (hasChangeOccurred)
        {
            _guiCommands.RefreshVariableValues();
        }

        return hasChangeOccurred;
    }

    public bool ShouldSkipDraggingMovementOn(InstanceSave instanceSave)
    {
        ElementWithState element = new ElementWithState(_selectedState.SelectedElement);

        List<ElementWithState> stack = new List<ElementWithState>() { element };

        var selectedInstances = _selectedState.SelectedInstances;

        bool shouldSkip = false;
        // Make sure this isn't attached to another instance
        var representation = _wireframeObjectManager.GetRepresentation(instanceSave, stack);

        if (representation != null && representation.Parent != null)
        {
            var parentRepresentation = representation.Parent;

            if (parentRepresentation != null)
            {
                var parentInstance = _wireframeObjectManager.GetInstance(parentRepresentation, InstanceFetchType.InstanceInCurrentElement, stack);

                if (selectedInstances.Contains(parentInstance))
                {
                    shouldSkip = true;
                }
            }
        }

        return shouldSkip;
    }


    public float ModifyVariable(string baseVariableName, float modificationAmount, InstanceSave instanceSave)
    {

        string nameWithInstance;
        object currentValueAsObject;
        GetCurrentValueForVariable(baseVariableName, instanceSave, out nameWithInstance, out currentValueAsObject);

        bool shouldContinue = true;

        if (_selectedState.CustomCurrentStateSave != null || currentValueAsObject == null)
        {
            // This is okay, we will do nothing here:
            shouldContinue = false;
        }

        if (shouldContinue)
        {
            var graphicalUiElement = _wireframeObjectManager.GetRepresentation(instanceSave, null);

            float currentValue = (float)currentValueAsObject;

            string unitsVariableName = baseVariableName + "Units";
            string unitsNameWithInstance;
            object unitsVariableAsObject;
            GetCurrentValueForVariable(unitsVariableName, instanceSave, out unitsNameWithInstance, out unitsVariableAsObject);

            if (float.IsPositiveInfinity(modificationAmount))
            {
                throw new InvalidOperationException("Cannot be infinite");
            }

            modificationAmount = ConvertAmountToPixelAccordingToUnitType(baseVariableName, modificationAmount, unitsVariableAsObject);

            if (float.IsPositiveInfinity(modificationAmount))
            {
                throw new InvalidOperationException("Cannot be infinite");
            }

            if (graphicalUiElement?.Parent?.GetAbsoluteFlipHorizontal() == true && baseVariableName == "X")
            {
                modificationAmount *= -1;
            }

            float newValue = currentValue + modificationAmount;
            _selectedState.SelectedStateSave.SetValue(nameWithInstance, newValue, instanceSave, "float");
            ElementSaveExtensions.ApplyVariableReferences(_selectedState.SelectedElement, _selectedState.SelectedStateSave);

            graphicalUiElement.SetProperty(baseVariableName, newValue);

            _wireframeObjectManager.RootGue?.ApplyVariableReferences(_selectedState.SelectedStateSave);

            _variableInCategoryPropagationLogic.PropagateVariablesInCategory(nameWithInstance,
                _selectedState.SelectedElement, _selectedState.SelectedStateCategorySave);


            return newValue;
        }
        else
        {
            return 0;
        }
    }

    public float ModifyVariable(string baseVariableName, float modificationAmount, ElementSave elementSave)
    {
        object currentValueAsObject;
        currentValueAsObject = GetCurrentValueForVariable(baseVariableName, null);

        float currentValue = (float)currentValueAsObject;
        string unitsVariableName = baseVariableName + " Units";
        string unitsNameWithInstance;
        object unitsVariableAsObject;
        GetCurrentValueForVariable(unitsVariableName, null, out unitsNameWithInstance, out unitsVariableAsObject);

        modificationAmount = ConvertAmountToPixelAccordingToUnitType(baseVariableName, modificationAmount, unitsVariableAsObject);

        float newValue = currentValue + modificationAmount;
        _selectedState.SelectedStateSave.SetValue(baseVariableName, newValue, null, "float");


        var ipso = _wireframeObjectManager.GetRepresentation(elementSave);
        ipso.SetProperty(baseVariableName, newValue);

        _variableInCategoryPropagationLogic.PropagateVariablesInCategory(baseVariableName,
            elementSave,
            _selectedState.SelectedStateCategorySave);

        return newValue;
    }


    public object GetCurrentValueForVariable(string baseVariableName, InstanceSave instanceSave)
    {
        string throwaway;
        object currentValueAsObject;
        GetCurrentValueForVariable(baseVariableName, instanceSave, out throwaway, out currentValueAsObject);
        return currentValueAsObject;
    }

    /// <summary>
    /// Returns the current value for a variable, considering inheritance and states. It returns the "effective" value of the variable.
    /// This value is in the current object's units.
    /// </summary>
    /// <param name="baseVariableName"></param>
    /// <param name="instanceSave"></param>
    /// <param name="nameWithInstance"></param>
    /// <param name="currentValue"></param>
    /// <returns></returns>
    private object GetCurrentValueForVariable(string baseVariableName, InstanceSave instanceSave, out string nameWithInstance, out object currentValue)
    {
        nameWithInstance = baseVariableName;

        currentValue = null;

        if (_selectedState.SelectedStateSave != null)
        {
            if (instanceSave != null)
            {
                nameWithInstance = instanceSave.Name + "." + baseVariableName;
                currentValue = _selectedState.SelectedStateSave.GetValueRecursive(nameWithInstance);
            }
            else
            {
                currentValue = _selectedState.SelectedStateSave.GetValueRecursive(nameWithInstance);
            }
        }

        return currentValue;
    }

    private float ConvertAmountToPixelAccordingToUnitType(string baseVariableName, float amount, object unitsVariableAsObject)
    {
        GeneralUnitType generalUnitType = UnitConverter.ConvertToGeneralUnit(unitsVariableAsObject);

        float xAmount;
        float yAmount;

        if (baseVariableName == "X" || baseVariableName == "Width")
        {
            xAmount = amount;
            yAmount = 0;
        }
        else
        {
            xAmount = 0;
            yAmount = amount;
        }

        if (generalUnitType == GeneralUnitType.PixelsFromMiddleInverted)
        {
            return amount * -1;
        }
        else if (generalUnitType != GeneralUnitType.PixelsFromLarge &&
            generalUnitType != GeneralUnitType.PixelsFromMiddle &&
            generalUnitType != GeneralUnitType.PixelsFromSmall &&
            generalUnitType != GeneralUnitType.PixelsFromBaseline)
        {

            float parentWidth;
            float parentHeight;
            float fileWidth;
            float fileHeight;
            float outX;
            float outY;


            var ipso = _wireframeObjectManager.GetSelectedRepresentation();
            ipso.GetFileWidthAndHeightOrDefault(out fileWidth, out fileHeight);
            ipso.GetParentWidthAndHeight(
                _projectManager.GumProjectSave.DefaultCanvasWidth, _projectManager.GumProjectSave.DefaultCanvasHeight,
                out parentWidth, out parentHeight);

            var unitsVariable = UnitConverter.ConvertToGeneralUnit(unitsVariableAsObject);

            UnitConverter.Self.ConvertToUnitTypeCoordinates(xAmount, yAmount, unitsVariable, unitsVariable,
                ipso.Width, ipso.Height,
                parentWidth, parentHeight,
                fileWidth, fileHeight,
                out outX, out outY);

            if (generalUnitType == GeneralUnitType.PercentageOfFile &&
                // If using the entire texture, the TextureWidth and TextureHeight values are ignored
                ipso.TextureAddress != TextureAddress.EntireTexture)
            {
                // need to amplify the value based on the ratio of what is displayed to the file size
                if (baseVariableName == "Width")
                {
                    var ratio = ipso.TextureWidth / fileWidth;

                    if (float.IsPositiveInfinity(ratio) == false && ratio != 0)
                    {
                        outX /= ratio;
                    }
                }
                if (baseVariableName == "Height")
                {
                    var ratio = ipso.TextureHeight / fileHeight;
                    if (float.IsPositiveInfinity(ratio) == false && ratio != 0)
                    {
                        outY /= ratio;
                    }
                }
            }

            // Values can become infinite. For example, if the Units is percent and the parent has a width or height of 0, then the value will be infinite.
            if (float.IsPositiveInfinity(outX) || float.IsNegativeInfinity(outX))
            {
                outX = 0;
            }
            if (float.IsPositiveInfinity(outY) || float.IsNegativeInfinity(outY))
            {
                outY = 0;
            }

            if (baseVariableName == "X" || baseVariableName == "Width")
            {
                return outX;
            }
            else
            {
                return outY;
            }
        }
        else
        {
            return amount;
        }
    }


    #endregion

    #region Category


    public StateSaveCategory AddCategory(IStateContainer objectToAddTo, string name)
    {
        if (objectToAddTo == null)
        {
            throw new Exception("Could not add category " + name + " because no element or behavior is selected");
        }



        StateSaveCategory category = new StateSaveCategory();
        category.Name = name;

        objectToAddTo.Categories.Add(category);


        // September 20, 2018
        // Not sure why we have
        // this category add itself
        // as a variable to the default
        // state. States can't set other
        // states, and I don't think the rest
        // of Gum depends on this. Commenting it
        // out to see.
        // Update - even though the element can't set
        // it's own categorized state in the default state,
        // instances use this variable to determine if a variable
        // should be shown.
        if (objectToAddTo is ElementSave)
        {              
            var elementToAddTo = objectToAddTo as ElementSave;
            elementToAddTo.DefaultState.Variables.Add(new VariableSave()
            {
                Name = category.Name + "State",
                // We used to set the type with the word "State" appended but why? Gum seems to not do this everywhere, and this can add confusion, so let's omit the "State" suffix
                Type = category.Name,
                Value = null
#if GUM
,
                CustomTypeConverter = new Gum.PropertyGridHelpers.Converters.AvailableStatesConverter(category.Name, _selectedState)
#endif
            });

            elementToAddTo.DefaultState.Variables.Sort((first, second) => first.Name.CompareTo(second.Name));
        }
        else if(objectToAddTo is BehaviorSave behaviorSave)
        {
            var componentsUsingBehavior = ObjectFinder.Self.GetElementsReferencing(behaviorSave).ToHashSet();

            foreach(var component in componentsUsingBehavior)
            {
                AddCategoriesFromBehavior(behaviorSave, component);

                _fileCommands.TryAutoSaveElement(component);
            }
        }

        _guiCommands.RefreshStateTreeView();

        _pluginManager.CategoryAdd(category);

        _fileCommands.TryAutoSaveCurrentObject();

        return category;
    }

    #endregion

    #region Behavior

    public BehaviorInstanceSave AddInstance(BehaviorSave behaviorToAddTo, string name, string type = null, string parentName = null)
    {
        if (behaviorToAddTo == null)
        {
            throw new Exception("Could not add instance named " + name + " because no element is selected");
        }


        var instanceSave = new BehaviorInstanceSave();
        instanceSave.Name = name;
        //instanceSave.ParentContainer = elementToAddTo;
        instanceSave.BaseType = type ?? StandardElementsManager.Self.DefaultType;
        behaviorToAddTo.RequiredInstances.Add(instanceSave);

        _pluginManager.BehaviorInstanceAdd(behaviorToAddTo, instanceSave);

        _guiCommands.RefreshElementTreeView(behaviorToAddTo);

        _wireframeObjectManager.RefreshAll(true);
        //_selectedState.SelectedInstance = instanceSave;

        // Set the parent before adding the instance in case plugins want to reject the creation of the object...
        //if (!string.IsNullOrEmpty(parentName))
        //{
        //    elementToAddTo.DefaultState.SetValue($"{instanceSave.Name}.Parent", parentName, "string");
        //}

        // We need to call InstanceAdd before we select the new object - the Undo manager expects it
        //_pluginManager.InstanceAdd(elementToAddTo, instanceSave);

        // a plugin may have removed this instance. If so, we need to refresh the tree node again:
        //if (elementToAddTo.Instances.Contains(instanceSave) == false)
        //{
        //    _guiCommands.RefreshElementTreeView(elementToAddTo);
        //    _wireframeObjectManager.RefreshAll(true);

        //    // August 2, 2022 - this is currently returned even if a plugin
        //    // removes the new instance. Should it be? Will it causes NullReferenceExceptions
        //    // on systems which always expect this to be non-null? Unsure....
        //    // August 4, 2022 - nope, this already is causing problems, we should return null.
        //    instanceSave = null;
        //}
        //else
        {
            _selectedState.SelectedInstance = instanceSave;
        }

        _fileCommands.TryAutoSaveBehavior(behaviorToAddTo);

        return instanceSave;
    }

    public void AddBehaviorTo(BehaviorSave behavior, ComponentSave componentSave, bool performSave = true)
    {
        AddBehaviorTo(behavior.Name, componentSave, performSave);
    }

    public void AddBehaviorTo(string behaviorName, ComponentSave componentSave, bool performSave = true)
    {
        var project = _projectManager.GumProjectSave;
        var behaviorSave = project.Behaviors.FirstOrDefault(item => item.Name == behaviorName);

        if(behaviorSave != null)
        {
            var behaviorReference = new ElementBehaviorReference();
            behaviorReference.BehaviorName = behaviorName;
            componentSave.Behaviors.Add(behaviorReference);

            AddCategoriesFromBehavior(behaviorSave, componentSave);

            _pluginManager.BehaviorReferencesChanged(componentSave);

            _guiCommands.PrintOutput($"Added behavior {behaviorName} to {componentSave}");

            if (performSave)
            {
                _fileCommands.TryAutoSaveElement(componentSave);
            }
        }
    }

    void AddCategoriesFromBehavior(BehaviorSave behaviorSave, ElementSave element)
    {
        foreach (var behaviorCategory in behaviorSave.Categories)
        {
            StateSaveCategory matchingComponentCategory =
                element.Categories.FirstOrDefault(item => item.Name == behaviorCategory.Name);

            if (matchingComponentCategory == null)
            {
                //category doesn't exist, so let's add a clone of it:

                // Use the AddCategory command so that it also gets variables:
                //matchingComponentCategory = new StateSaveCategory();
                //matchingComponentCategory.Name = behaviorCategory.Name;
                //element.Categories.Add(matchingComponentCategory);
                matchingComponentCategory = AddCategory(element, behaviorCategory.Name);
            }

            foreach (var behaviorState in behaviorCategory.States)
            {
                var matchingComponentState =
                    matchingComponentCategory.States.FirstOrDefault(item => item.Name == behaviorState.Name);

                if (matchingComponentState == null)
                {
                    // state doesn't exist, so add it:
                    var newState = new StateSave();
                    newState.Name = behaviorState.Name;
                    newState.ParentContainer = element;
                    matchingComponentCategory.States.Add(newState);
                }
            }
        }
    }


    #endregion


}
