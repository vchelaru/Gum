using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using GumDataTypes.Variables;

namespace Gum.DataTypes;

/// <summary>
/// Class that can find variables
/// and values recursively.  There's
/// so many different ways that this
/// happens that this consolidates all
/// logic in one place
/// </summary>
public class RecursiveVariableFinder : IVariableFinder
{
    #region Enums

    public enum VariableContainerType
    {
        InstanceSave,
        StateSave
    }

    #endregion

    #region Fields

    InstanceSave mInstanceSave;
    public InstanceSave InstanceSave => mInstanceSave;
    public List<ElementWithState> ElementStack { get; private set; }
    StateSave mStateSave;




    #endregion

    #region Properties


    public VariableContainerType ContainerType
    {
        get;
        private set;
    }


    #endregion

    /// <summary>
    /// Creates a new RecursiveVariableFinder for the argument InstanceSave and Container. The InstanceSave
    /// should not be null.
    /// </summary>
    /// <remarks>
    /// If a RecursiveVariableFinder is created with an instance parameter, then GetValue calls
    /// should be performed with the unqualified variable name. In other words, to get the X value
    /// on the instnace, the "X" value should be passed rather than "InstanceName.X"
    /// </remarks>
    /// <param name="instanceSave">The InstanceSave which has variables that will be requested.</param>
    /// <param name="container">The container of the instance.</param>
    /// <exception cref="ArgumentException">Thrown if the instance is null</exception>
    public RecursiveVariableFinder(InstanceSave instanceSave, ElementSave container)
    {
        if (instanceSave == null)
        {
            throw new ArgumentException("InstanceSave must not be null", "instanceSave");
        }

        ContainerType = VariableContainerType.InstanceSave;

        mInstanceSave = instanceSave;

        // October 11, 2023
        // Vic asks - if the RFV is for an instance in a container, should the instance name be set on the
        // elementWithState? The GetVariable method seems to expect that, so let's try that:
        // Update - the reason this works this way is - if the ElementWithState has an instance, then the name
        // of the instance is automatically used on the variable. In other words, if an instance is assigned, and we want
        // the X value on the instance, then we should ask the RecursiveVariableFinder for "X".
        // If no instance is assigned, then we would ask for "InstanceName.X". However, since the
        // constructor here explicitly assigns an instance, I think it's proper to assign the instance
        // internally and let the user only pass "X"

        var elementWithState = new ElementWithState(container);
        elementWithState.InstanceName = instanceSave?.Name;
        ElementStack = new List<ElementWithState>() { elementWithState };
    }

    public RecursiveVariableFinder(InstanceSave instanceSave, List<ElementWithState> elementStack)
    {
        if (instanceSave == null)
        {
            throw new ArgumentException("InstanceSave must not be null", "instanceSave");
        }

        ContainerType = VariableContainerType.InstanceSave;

        mInstanceSave = instanceSave;
        ElementStack = elementStack;
    }

    public RecursiveVariableFinder(List<ElementWithState> elementStack)
    {


        var last = elementStack.Last();
        mStateSave = last.StateSave;
        ElementStack = elementStack;

        // technically the element stack could include elements that have instances. However, 
        // the element stack could have a mix where some ElementWithStates have instances, and 
        // some do not. therefore, we can't assume the entire thing uses instances. 
        ContainerType = VariableContainerType.StateSave;
    }

    public RecursiveVariableFinder(StateSave stateSave)
    {
#if FULL_DIAGNOSTICS
        if (stateSave == null)
        {
            throw new ArgumentNullException(nameof(stateSave));
        }

        if (stateSave.ParentContainer == null)
        {
            throw new NullReferenceException("The state passed in to the RecursiveVariableFinder has a null ParentContainer and it shouldn't");
        }
#endif
        ContainerType = VariableContainerType.StateSave;
        mStateSave = stateSave;

        ElementStack = new List<ElementWithState>();


        ElementStack.Add(new ElementWithState(stateSave.ParentContainer) { StateName = stateSave.Name });
    }

    public object GetValue(string variableName)
    {
        switch (ContainerType)
        {
            case VariableContainerType.InstanceSave:

#if FULL_DIAGNOSTICS
                if (ElementStack.Count != 0)
                {
                    if (ElementStack.Last().Element == null)
                    {
                        throw new InvalidOperationException("The ElementStack contains an ElementWithState with no Element");
                    }
                }
#endif

                VariableSave variable = GetVariable(variableName);
                if (variable != null)
                {
                    return variable.Value;
                }
                else
                {
                    return null;
                }

            //return mInstanceSave.GetValueFromThisOrBase(mElementStack, variableName);
            //break;
            case VariableContainerType.StateSave:
                return mStateSave.GetValueRecursive(variableName);
            //break;
        }

        throw new NotImplementedException();
    }

    public T GetValue<T>(string variableName)
    {
#if FULL_DIAGNOSTICS
        if ( ElementStack.Count != 0)
        {
            if (ElementStack.Last().Element == null)
            {
                throw new InvalidOperationException("The ElementStack contains an ElementWithState with no Element");
            }
        }
#endif


        object valueAsObject = GetValue(variableName);
        if (valueAsObject is T)
        {
            return (T)valueAsObject;
        }
        else
        {
            return default(T);
        }
    }

    public VariableSave GetVariable(string variableName)
    {
        switch (ContainerType)
        {
            case VariableContainerType.InstanceSave:

                string instanceName = null;
                if (ElementStack.Count != 0)
                {
                    // October 11, 2023
                    // This is intentionally using the stack and not the InstanceName on the RFV. What's the difference?
                    // I'm going to rely on the stack since this code is old and I'm going to modify the constructor.
                    instanceName = ElementStack.Last().InstanceName;
                }

                bool onlyIfSetsValue = false;

#if FULL_DIAGNOSTICS
                if (ElementStack.Count != 0)
                {
                    if (ElementStack.Last().Element == null)
                    {
                        throw new InvalidOperationException("The ElementStack contains an ElementWithState with no Element");
                    }
                }
#endif

                var found = mInstanceSave.GetVariableFromThisOrBase(ElementStack, this, variableName, false, onlyIfSetsValue);
                if (found != null && !string.IsNullOrEmpty(found.ExposedAsName))
                {
                    var allExposed = GetExposedVariablesForThisInstance(mInstanceSave, instanceName, ElementStack, variableName);
                    var exposed = allExposed.FirstOrDefault();

                    if (exposed != null && exposed.Value != null)
                    {
                        found = exposed;
                    }
                }

                if (found == null || found.SetsValue == false || found.Value == null)
                {
                    onlyIfSetsValue = true;
                    found = mInstanceSave.GetVariableFromThisOrBase(ElementStack, this, variableName, false, true);
                }

                return found;
            case VariableContainerType.StateSave:
                return mStateSave.GetVariableRecursive(variableName);
            //break;
        }
        throw new NotImplementedException();
    }

    public VariableListSave GetVariableList(string variableName)
    {
        switch (ContainerType)
        {
            case VariableContainerType.InstanceSave:
                return mInstanceSave.GetVariableListFromThisOrBase(ElementStack.Last().Element, variableName);
            case VariableContainerType.StateSave:
                return mStateSave.GetVariableListRecursive(variableName);
            //break;
        }
        throw new NotImplementedException();
    }

    public List<VariableSave> GetExposedVariablesForThisInstance(DataTypes.InstanceSave instance, string parentInstanceName, 
        List<ElementWithState> elementStack, string requiredName)
    {
        List<VariableSave> exposedVariables = new List<VariableSave>();
        if (elementStack.Count > 1)
        {
            ElementWithState containerOfVariables = elementStack[elementStack.Count - 2];
            ElementWithState definerOfVariables = elementStack[elementStack.Count - 1];

            foreach (VariableSave variable in definerOfVariables.Element.DefaultState.Variables.Where(
                item => !string.IsNullOrEmpty(item.ExposedAsName) && item.GetRootName() == requiredName))
            {
                if (variable.SourceObject == instance.Name)
                {
                    // This variable is exposed, let's see if the container does anything with it

                    VariableSave foundVariable = containerOfVariables.StateSave.GetVariableRecursive(
                        parentInstanceName + "." + variable.ExposedAsName);

                    if (foundVariable != null)
                    {
                        if (!string.IsNullOrEmpty(foundVariable.ExposedAsName))
                        {
                            // This variable is itself exposed, so we should go up one level to see 
                            // what's going on.
                            var instanceInParent = containerOfVariables.Element.GetInstance(parentInstanceName);
                            var parentparentInstanceName = containerOfVariables.InstanceName;

                            List<ElementWithState> stackWithLastRemoved = new List<ElementWithState>();
                            stackWithLastRemoved.AddRange(elementStack);
                            stackWithLastRemoved.RemoveAt(stackWithLastRemoved.Count - 1);

                            var exposedExposed = GetExposedVariablesForThisInstance(instanceInParent, parentparentInstanceName, 
                                stackWithLastRemoved,
                                // This used to be this:
                                //foundVariable.ExposedAsName
                                // But it should be this:
                                variable.ExposedAsName
                                );

                            if (exposedExposed.Count != 0)
                            {
                                foundVariable = exposedExposed.First();
                            }

                        }
                        
                        VariableSave variableToAdd = new VariableSave();
                        variableToAdd.Type = variable.Type;
                        variableToAdd.Value = foundVariable.Value;
                        variableToAdd.SetsValue = foundVariable.SetsValue;
                        variableToAdd.Name = variable.Name.Substring(variable.Name.IndexOf('.') + 1);
                        exposedVariables.Add(variableToAdd);
                    }
                }
            }
        }

        return exposedVariables;
    }

    /// <summary>
    /// Returns the value of the variable from the bottom of the stack by climbing back up to find the most derived assignment
    /// </summary>
    public object GetValueByBottomName(string variableName, int? currentStackIndex = null)
    {
        int stackIndex = currentStackIndex ?? this.ElementStack.Count - 1;

        ElementWithState itemBefore = null;

        // This assumes that the ElementStack has all of the instances leading down to the Text instance.
        // For example, the element stack may have:
        // [0]: MainMenu.StartGameButton
        // [1]: Button.TexInstance
        // [2]: Text

        if(stackIndex > 0)
        {
            itemBefore = this.ElementStack[stackIndex - 1];
        }

        string variableNameAbove = null;

        if(itemBefore != null)
        {
            if(variableName.Contains("."))
            {
                var element = ElementStack[stackIndex].Element;
                var exposed = element.DefaultState.Variables
                    .FirstOrDefault(item => item.Name == variableName && !string.IsNullOrEmpty(item.ExposedAsName));

                if(exposed != null)
                {
                    variableNameAbove = itemBefore.InstanceName + "." + exposed.ExposedAsName;
                }
            }
            else
            {
                variableNameAbove = itemBefore.InstanceName + "." + variableName;
            }
        }

        object fromAbove = null;
        if(variableNameAbove != null)
        {
            fromAbove = GetValueByBottomName(variableNameAbove, stackIndex - 1);
        }
        if(fromAbove != null)
        {
            return fromAbove;
        }
        else
        {
            var tempValue = GetValue(variableName);

            // This doesn't seem to consider states being assigned throughout the stack, but using
            // GetValueRecursive does work that way:
            //return ElementStack[stackIndex].StateSave.GetValue(variableName);
            return ElementStack[stackIndex].StateSave.GetValueRecursive(variableName);

        }

    }
}
