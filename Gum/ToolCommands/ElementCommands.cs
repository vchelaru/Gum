﻿using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.PropertyGridHelpers;
using Gum.DataTypes.Behaviors;
using System.ComponentModel;
using Gum.Wireframe;
using GumRuntime;
using Gum.Converters;
using Gum.RenderingLibrary;
using RenderingLibrary;

namespace Gum.ToolCommands
{
    public class ElementCommands
    {
        #region Fields

        static ElementCommands mSelf;

        #endregion

        #region Properties

        public static ElementCommands Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ElementCommands();
                }
                return mSelf;
            }
        }

        #endregion

        #region Instance

        public InstanceSave AddInstance(ElementSave elementToAddTo, string name, string type = null, string parentName = null)
        {
            InstanceSave instanceSave = new InstanceSave();
            instanceSave.Name = name;
            instanceSave.ParentContainer = elementToAddTo;
            instanceSave.BaseType = type ?? StandardElementsManager.Self.DefaultType;

            return AddInstance(elementToAddTo, instanceSave, parentName);
        }

        public InstanceSave AddInstance(ElementSave elementToAddTo, InstanceSave instanceSave, string parentName = null)
        {
            if (elementToAddTo == null)
            {
                throw new Exception("Could not add instance named " + instanceSave.Name + " because no element is selected");
            }

            elementToAddTo.Instances.Add(instanceSave);

            GumCommands.Self.GuiCommands.RefreshElementTreeView(elementToAddTo);

            Wireframe.WireframeObjectManager.Self.RefreshAll(true);
            //SelectedState.Self.SelectedInstance = instanceSave;

            // Set the parent before adding the instance in case plugins want to reject the creation of the object...
            if (!string.IsNullOrEmpty(parentName))
            {
                elementToAddTo.DefaultState.SetValue($"{instanceSave.Name}.Parent", parentName, "string");
            }

            // We need to call InstanceAdd before we select the new object - the Undo manager expects it
            PluginManager.Self.InstanceAdd(elementToAddTo, instanceSave);

            // a plugin may have removed this instance. If so, we need to refresh the tree node again:
            if (elementToAddTo.Instances.Contains(instanceSave) == false)
            {
                GumCommands.Self.GuiCommands.RefreshElementTreeView(elementToAddTo);
                Wireframe.WireframeObjectManager.Self.RefreshAll(true);

                // August 2, 2022 - this is currently returned even if a plugin
                // removes the new instance. Should it be? Will it causes NullReferenceExceptions
                // on systems which always expect this to be non-null? Unsure....
                // August 4, 2022 - nope, this already is causing problems, we should return null.
                instanceSave = null;
            }
            else
            {
                SelectedState.Self.SelectedInstance = instanceSave;
            }

            GumCommands.Self.FileCommands.TryAutoSaveElement(elementToAddTo);

            return instanceSave;
        }

        /// <summary>
        /// Removes the argument instance from the argument elementToRemoveFrom, and detaches any
        /// object that was attached to this parent.
        /// </summary>
        /// <param name="instanceToRemove">The instance to remove.</param>
        /// <param name="elementToRemoveFrom">The element to remove from.</param>
        public void RemoveInstance(InstanceSave instanceToRemove, ElementSave elementToRemoveFrom)
        {
            if (!elementToRemoveFrom.Instances.Contains(instanceToRemove))
            {
                throw new Exception("Could not find the instance " + instanceToRemove.Name + " in " + elementToRemoveFrom.Name);
            }

            elementToRemoveFrom.Instances.Remove(instanceToRemove);

            RemoveParentReferencesToInstance(instanceToRemove, elementToRemoveFrom);

            elementToRemoveFrom.Events.RemoveAll(item => item.GetSourceObject() == instanceToRemove.Name);


            PluginManager.Self.InstanceDelete(elementToRemoveFrom, instanceToRemove);

            if (SelectedState.Self.SelectedInstance == instanceToRemove)
            {
                SelectedState.Self.SelectedInstance = null;
            }
        }

        public void RemoveInstances(List<InstanceSave> instances, ElementSave elementToRemoveFrom)
        {
            foreach(var instance in instances)
            {
                elementToRemoveFrom.Instances.Remove(instance);
                RemoveParentReferencesToInstance(instance, elementToRemoveFrom);
                elementToRemoveFrom.Events.RemoveAll(item => item.GetSourceObject() == instance.Name);
            }


            PluginManager.Self.InstancesDelete(elementToRemoveFrom, instances.ToArray());

            var newSelection = SelectedState.Self.SelectedInstances.ToList()
                .Except(instances);
            SelectedState.Self.SelectedInstances = newSelection;
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
            AddState(stateContainer, category, stateSave);

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
                    VariableInCategoryPropagationLogic.Self
                        .PropagateVariablesInCategory(variable.Name, elementSave, category);
                }
            }


            PluginManager.Self.StateAdd(stateSave);

            GumCommands.Self.GuiCommands.RefreshStateTreeView();

            if(stateContainer is BehaviorSave behavior)
            {
                GumCommands.Self.FileCommands.TryAutoSaveBehavior(behavior);
            }
            else
            {
                GumCommands.Self.FileCommands.TryAutoSaveElement(stateContainer as ElementSave);
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

        public void RemoveState(StateSave stateSave, IStateContainer elementToRemoveFrom)
        {
            
            elementToRemoveFrom.UncategorizedStates.Remove(stateSave);

            foreach (var category in elementToRemoveFrom.Categories.Where(item => item.States.Contains(stateSave)))
            {
                category.States.Remove(stateSave);
            }

            if(elementToRemoveFrom is BehaviorSave behaviorSave)
            {
                GumCommands.Self.FileCommands.TryAutoSaveBehavior(behaviorSave);
            }
            else if(elementToRemoveFrom is ElementSave elementSave)
            {
                GumCommands.Self.FileCommands.TryAutoSaveElement(elementSave);
            }
        }
        #endregion

        #region Variables

        public void SortVariables()
        {
            var gumProject = GumState.Self.ProjectState.GumProjectSave;

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
            var hasSelection = SelectedState.Self.SelectedComponent != null ||
                SelectedState.Self.SelectedStandardElement != null ||
                SelectedState.Self.SelectedInstance != null;

            if (hasSelection)
            {
                var isMovingElement = SelectedState.Self.SelectedInstances.Count() == 0 &&
                    (SelectedState.Self.SelectedComponent != null || SelectedState.Self.SelectedStandardElement != null);

                if (isMovingElement)
                {
                    var element = SelectedState.Self.SelectedElement;
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
                    var selectedInstances = SelectedState.Self.SelectedInstances;

                    foreach (InstanceSave instance in selectedInstances)
                    {
                        bool shouldSkip = ShouldSkipDraggingMovementOn(instance);

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
                    GumCommands.Self.GuiCommands.RefreshVariableValues();
                }
            }

            return hasChangeOccurred;
        }

        public bool ShouldSkipDraggingMovementOn(InstanceSave instanceSave)
        {
            ElementWithState element = new ElementWithState(SelectedState.Self.SelectedElement);

            List<ElementWithState> stack = new List<ElementWithState>() { element };

            var selectedInstances = SelectedState.Self.SelectedInstances;

            bool shouldSkip = false;
            // Make sure this isn't attached to another instance
            var representation = WireframeObjectManager.Self.GetRepresentation(instanceSave, stack);

            if (representation != null && representation.Parent != null)
            {
                var parentRepresentation = representation.Parent;

                if (parentRepresentation != null)
                {
                    var parentInstance = WireframeObjectManager.Self.GetInstance(parentRepresentation, InstanceFetchType.InstanceInCurrentElement, stack);

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

            if (SelectedState.Self.CustomCurrentStateSave != null || currentValueAsObject == null)
            {
                // This is okay, we will do nothing here:
                shouldContinue = false;
            }

            if (shouldContinue)
            {
                var graphicalUiElement = WireframeObjectManager.Self.GetRepresentation(instanceSave, null);

                float currentValue = (float)currentValueAsObject;

                string unitsVariableName = baseVariableName + " Units";
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
                SelectedState.Self.SelectedStateSave.SetValue(nameWithInstance, newValue, instanceSave, "float");
                ElementSaveExtensions.ApplyVariableReferences(SelectedState.Self.SelectedElement, SelectedState.Self.SelectedStateSave);

                graphicalUiElement.SetProperty(baseVariableName, newValue);

                WireframeObjectManager.Self.RootGue?.ApplyVariableReferences(SelectedState.Self.SelectedStateSave);

                VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(nameWithInstance,
                    GumState.Self.SelectedState.SelectedElement, GumState.Self.SelectedState.SelectedStateCategorySave);


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
            SelectedState.Self.SelectedStateSave.SetValue(baseVariableName, newValue, null, "float");


            var ipso = WireframeObjectManager.Self.GetRepresentation(elementSave);
            ipso.SetProperty(baseVariableName, newValue);

            VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(baseVariableName,
                elementSave,
                GumState.Self.SelectedState.SelectedStateCategorySave);

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
        private static object GetCurrentValueForVariable(string baseVariableName, InstanceSave instanceSave, out string nameWithInstance, out object currentValue)
        {
            nameWithInstance = baseVariableName;

            currentValue = null;

            if (SelectedState.Self.SelectedStateSave != null)
            {
                if (instanceSave != null)
                {
                    nameWithInstance = instanceSave.Name + "." + baseVariableName;
                    currentValue = SelectedState.Self.SelectedStateSave.GetValueRecursive(nameWithInstance);
                }
                else
                {
                    currentValue = SelectedState.Self.SelectedStateSave.GetValueRecursive(nameWithInstance);
                }
            }

            return currentValue;
        }

        private static float ConvertAmountToPixelAccordingToUnitType(string baseVariableName, float amount, object unitsVariableAsObject)
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


                var ipso = WireframeObjectManager.Self.GetSelectedRepresentation();
                ipso.GetFileWidthAndHeightOrDefault(out fileWidth, out fileHeight);
                ipso.GetParentWidthAndHeight(
                    ProjectManager.Self.GumProjectSave.DefaultCanvasWidth, ProjectManager.Self.GumProjectSave.DefaultCanvasHeight,
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
                    CustomTypeConverter = new Gum.PropertyGridHelpers.Converters.AvailableStatesConverter(category.Name)
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

                    GumCommands.Self.FileCommands.TryAutoSaveElement(component);
                }
            }

            ElementTreeViewManager.Self.RefreshUi(SelectedState.Self.SelectedStateContainer);

            GumCommands.Self.GuiCommands.RefreshStateTreeView();

            PluginManager.Self.CategoryAdd(category);

            GumCommands.Self.FileCommands.TryAutoSaveCurrentObject();

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

            GumCommands.Self.GuiCommands.RefreshElementTreeView(behaviorToAddTo);

            Wireframe.WireframeObjectManager.Self.RefreshAll(true);
            //SelectedState.Self.SelectedInstance = instanceSave;

            // Set the parent before adding the instance in case plugins want to reject the creation of the object...
            //if (!string.IsNullOrEmpty(parentName))
            //{
            //    elementToAddTo.DefaultState.SetValue($"{instanceSave.Name}.Parent", parentName, "string");
            //}

            // We need to call InstanceAdd before we select the new object - the Undo manager expects it
            //PluginManager.Self.InstanceAdd(elementToAddTo, instanceSave);

            // a plugin may have removed this instance. If so, we need to refresh the tree node again:
            //if (elementToAddTo.Instances.Contains(instanceSave) == false)
            //{
            //    GumCommands.Self.GuiCommands.RefreshElementTreeView(elementToAddTo);
            //    Wireframe.WireframeObjectManager.Self.RefreshAll(true);

            //    // August 2, 2022 - this is currently returned even if a plugin
            //    // removes the new instance. Should it be? Will it causes NullReferenceExceptions
            //    // on systems which always expect this to be non-null? Unsure....
            //    // August 4, 2022 - nope, this already is causing problems, we should return null.
            //    instanceSave = null;
            //}
            //else
            {
                SelectedState.Self.SelectedInstance = instanceSave;
            }

            GumCommands.Self.FileCommands.TryAutoSaveBehavior(behaviorToAddTo);

            return instanceSave;
        }

        public void AddBehaviorTo(BehaviorSave behavior, ComponentSave componentSave, bool performSave = true)
        {
            AddBehaviorTo(behavior.Name, componentSave, performSave);
        }

        public void AddBehaviorTo(string behaviorName, ComponentSave componentSave, bool performSave = true)
        {
            var project = ProjectManager.Self.GumProjectSave;
            var behaviorSave = project.Behaviors.FirstOrDefault(item => item.Name == behaviorName);

            if(behaviorSave != null)
            {
                var behaviorReference = new ElementBehaviorReference();
                behaviorReference.BehaviorName = behaviorName;
                componentSave.Behaviors.Add(behaviorReference);

                GumCommands.Self.ProjectCommands.ElementCommands.AddCategoriesFromBehavior(behaviorSave, componentSave);

                PluginManager.Self.BehaviorReferencesChanged(componentSave);

                GumCommands.Self.GuiCommands.PrintOutput($"Added behavior {behaviorName} to {componentSave}");

                if (performSave)
                {
                    GumCommands.Self.FileCommands.TryAutoSaveElement(componentSave);
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

        public void RemoveParentReferencesToInstance(InstanceSave instanceToRemove, ElementSave elementToRemoveFrom)
        {
            foreach (StateSave stateSave in elementToRemoveFrom.AllStates)
            {
                for (int i = stateSave.Variables.Count - 1; i > -1; i--)
                {
                    var variable = stateSave.Variables[i];

                    if (variable.SourceObject == instanceToRemove.Name)
                    {
                        // this is a variable that assigns a value on the removed object. The object
                        // is gone, so the variable should be removed too.
                        stateSave.Variables.RemoveAt(i);
                    }
                    else if (variable.GetRootName() == "Parent" && variable.Value as string == instanceToRemove.Name)
                    {
                        // This is a variable that assigns the Parent to the removed object. Since the object is
                        // gone, the parent value shouldn't be assigned anymore.
                        stateSave.Variables.RemoveAt(i);
                    }
                }
                for (int i = stateSave.VariableLists.Count - 1; i > -1; i--)
                {
                    if (stateSave.VariableLists[i].SourceObject == instanceToRemove.Name)
                    {
                        stateSave.VariableLists.RemoveAt(i);
                    }
                }
            }
        }

    }
}
