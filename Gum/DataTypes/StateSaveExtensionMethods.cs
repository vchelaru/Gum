using Gum.Managers;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using ToolsUtilities;
//using Gum.Wireframe;


//using Gum.Reflection;


namespace Gum.DataTypes.Variables;

public static class StateSaveExtensionMethods
{
    /// <summary>
    /// Fixes enumeration values and sorts all variables alphabetically
    /// </summary>
    /// <param name="stateSave">The state to initialize.</param>
    public static void Initialize(this StateSave stateSave)
    {
        foreach (VariableSave variable in stateSave.Variables)
        {
            variable.FixEnumerations();
        }
        stateSave.Variables.Sort((a, b) => a.Name.CompareTo(b.Name));
    }

    /// <summary>
    /// Returns the value of the variable name from this state. If not found, will follow inheritance to find 
    /// the value from the base.
    /// </summary>
    /// <param name="stateSave">The state in the current element.</param>
    /// <param name="variableName">The variable name</param>
    /// <returns>The value found recursively, where the most-derived value has priority.</returns>
    public static object GetValueRecursive(this StateSave stateSave, string variableName)
    {
        // First we check if this state sets the value directly...
        object value = stateSave.GetValue(variableName);

        ElementSave elementContainingState = stateSave.ParentContainer;
        if (value == null && elementContainingState != null)
        {
            // See if variableName is an alias from exposing:
            //var variable = elementContainingState.GetVariableFromThisOrBase(variableName);
            var variable = stateSave.GetVariableRecursive(variableName);

            // in case it is an exposed variable:
            if (variable != null && variableName.Contains(".") == false && variableName != variable.Name)
            {
                value = stateSave.GetValue(variable.Name);
            }
        }

        if (value == null)
        {

            var foundVariable = stateSave.GetVariableRecursive(variableName);

            bool wasFound = false;
            if (elementContainingState != null && stateSave != elementContainingState.DefaultState)
            {
                // try to get it from the stateSave recursively since it's not set directly on the state...
                if (foundVariable != null && foundVariable.SetsValue && foundVariable.Value != null)
                {
                    // Why do we early out here?
                    //return foundVariable.Value;
                    value = foundVariable.Value;
                    wasFound = true;
                }

                if (!wasFound)
                {
                    var foundVariableList = stateSave.GetVariableListRecursive(variableName);
                    if (foundVariableList?.ValueAsIList != null)
                    {
                        value = foundVariableList.ValueAsIList;
                        wasFound = true;
                    }
                }
            }

            // The variable could be "LabelVisible", but the rest of this method expects
            // the name to include '.' and not have the exposed alias:
            if (variableName.Contains(".") == false && foundVariable?.ExposedAsName != null)
            {
                variableName = foundVariable.Name;
            }

            string nameInBase = variableName;

            if (StringFunctions.ContainsNoAlloc(variableName, '.'))
            {
                // this variable is set on an instance, but we're going into the
                // base type, so we want to get the raw variable and not the variable
                // as tied to an instance.
                nameInBase = variableName.Substring(nameInBase.IndexOf('.') + 1);
            }
            if (!wasFound)
            {
                // it hasn't been found on this state directly or recursively, but maybe there is a variable
                // set on the instance which then sets the value, so we need to follow those
                var sourceObjectName = VariableSave.GetSourceObject(variableName);
                var instance = elementContainingState?.Instances.FirstOrDefault(item => item.Name == sourceObjectName);
                if (instance != null)
                {

                    var instanceType = ObjectFinder.Self.GetElementSave(instance);

                    if (instanceType != null)
                    {
                        // Calling matchingState.GetValueRecursive will always return a value, so the first state will return true.
                        // Therefore, we shouldn't do a recrusive call here, at least not initially. We should do non-recursive. If we 
                        // don't find something in the non-recursive, then we should do a recursive call.

                        for (int i = 0; i < stateSave.Variables.Count; i++)
                        {
                            var instanceStateVariable = stateSave.Variables[i];
                            if (instanceStateVariable.SourceObject == sourceObjectName && instanceStateVariable.SetsValue &&
                                // check this last since it's the slowest:
                                instanceStateVariable.IsState(elementContainingState))
                            {
                                var matchingState = instanceType.AllStates.FirstOrDefault(item => item.Name == (string)instanceStateVariable.Value);

                                if (matchingState != null)
                                {
                                    value = matchingState.GetValue(nameInBase);
                                    wasFound = value != null;
                                    if (wasFound)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        // Now that we did non-recursive (top level only), then let's do recursive:
                        if (!wasFound)
                        {
                            for (int i = 0; i < stateSave.Variables.Count; i++)
                            {
                                var instanceStateVariable = stateSave.Variables[i];
                                if (instanceStateVariable.SourceObject == sourceObjectName && instanceStateVariable.SetsValue &&
                                    // check this last since it's the slowest:
                                    instanceStateVariable.IsState(elementContainingState))
                                {
                                    var matchingState = instanceType.AllStates.FirstOrDefault(item => item.Name == (string)instanceStateVariable.Value);

                                    if (matchingState != null)
                                    {
                                        value = matchingState.GetValueRecursive(nameInBase);
                                        wasFound = value != null;
                                        if (wasFound)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < stateSave.Variables.Count; i++)
                    {
                        var stateVariable = stateSave.Variables[i];
                        ElementSave categoryOwner = null;
                        StateSaveCategory category = null;
                        if (string.IsNullOrEmpty(stateVariable.SourceObject) && stateVariable.SetsValue &&
                            stateVariable.Value is string stateName &&
                            // check this last since it's the slowest:
                            stateVariable.IsState(elementContainingState, out categoryOwner, out category) && category != null)
                        {
                            var foundBaseDefinedState = category.States.FirstOrDefault(item => item.Name == stateName);
                            value = foundBaseDefinedState?.GetValue(nameInBase);
                            wasFound = value != null;
                            if (wasFound)
                            {
                                break;
                            }
                            int m = 3;
                        }
                    }
                }
            }

            if (!wasFound && elementContainingState != null)
            {
                if (!string.IsNullOrEmpty(elementContainingState.BaseType))
                {
                    // eventually pass the state, but for now use default
                    value = TryToGetValueFromInheritance(variableName, elementContainingState.BaseType);
                }

                if (value == null)
                {
                    ElementSave baseElement = GetBaseElementFromVariable(variableName, elementContainingState);

                    if (baseElement != null)
                    {


                        value = baseElement.DefaultState.GetValueRecursive(nameInBase);
                    }
                }


                if (value == null && elementContainingState is ComponentSave)
                {
                    StateSave defaultStateForComponent = StandardElementsManager.Self.GetDefaultStateFor("Component");
                    if (defaultStateForComponent != null)
                    {
                        value = defaultStateForComponent.GetValueRecursive(variableName);
                    }
                }
            }
        }


        return value;
    }


    private static object TryToGetValueFromInheritance(string variableName, string baseType)
    {
        object foundValue = null;

        var baseElement = ObjectFinder.Self.GetElementSave(baseType);

        if (baseElement?.DefaultState != null)
        {
            var variable = baseElement.DefaultState.GetVariableSave(variableName);

            if (variable?.SetsValue == true)
            {
                foundValue = variable.Value;
            }
        }

        if (foundValue == null && !string.IsNullOrEmpty(baseElement?.BaseType))
        {
            foundValue = TryToGetValueFromInheritance(variableName, baseElement.BaseType);
        }

        return foundValue;
    }

    private static ElementSave GetBaseElementFromVariable(string variableName, ElementSave parent)
    {
        // this thing is the default state
        // But it's null, so we have to look
        // to the parent
        ElementSave baseElement = null;

        if (StringFunctions.ContainsNoAlloc(variableName, '.'))
        {
            string instanceToSearchFor = variableName.Substring(0, variableName.IndexOf('.'));

            InstanceSave instanceSave = parent.GetInstance(instanceToSearchFor);

            if (instanceSave != null)
            {
                baseElement = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);
            }
        }
        else
        {
            baseElement = ObjectFinder.Self.GetElementSave(parent.BaseType);
        }
        return baseElement;
    }


    /// <summary>
    /// Returns the first instance of an existing VariableSave recursively. Returns null if not found.
    /// </summary>
    /// <param name="stateSave">The possible state that contains the variable. 
    /// If it doesn't, then the code will recursively go to base types.</param>
    /// <param name="variableName"></param>
    /// <returns></returns>
    public static VariableSave? GetVariableRecursive(this StateSave stateSave, string variableName)
    {
        VariableSave? variableSave = stateSave.GetVariableSave(variableName);

        if (variableSave == null)
        {
            // 1. Go to the default state if it's not a default
            bool shouldGoToDefaultState = false;
            // 2. Go to the base type if the variable is on the container itself, or if the instance is DefinedByBase
            bool shouldGoToBaseType = false;
            // 3. Go to the instance if it's on an instance and we're not going to the default state or base type
            bool shouldGoToInstanceComponent = false;

            // Is this thing the default?
            ElementSave elementContainingState = stateSave.ParentContainer;

            if (elementContainingState != null)
            {
                if (elementContainingState != null && stateSave != elementContainingState.DefaultState)
                {
                    shouldGoToDefaultState = true;
                }

                var isVariableOnInstance = variableName.Contains('.');
                InstanceSave instance = null;
                bool canGoToBase = false;

                var hasBaseType = !string.IsNullOrEmpty(elementContainingState.BaseType);
                var isVariableDefinedOnThisInheritanceLevel = false;

                var instanceName = VariableSave.GetSourceObject(variableName);
                instance = elementContainingState.Instances.FirstOrDefault(item => item.Name == instanceName);

                if (isVariableOnInstance && hasBaseType)
                {
                    if (instance != null && instance.DefinedByBase == false)
                    {
                        isVariableDefinedOnThisInheritanceLevel = true;
                    }
                }
                else if (!hasBaseType)
                {
                    isVariableDefinedOnThisInheritanceLevel = true;
                }

                canGoToBase = isVariableOnInstance == false ||
                    isVariableDefinedOnThisInheritanceLevel == false;

                if (!shouldGoToDefaultState)
                {
                    shouldGoToBaseType = canGoToBase;
                }

                if (!shouldGoToDefaultState && !shouldGoToBaseType)
                {
                    shouldGoToInstanceComponent = isVariableOnInstance;
                }


                if (shouldGoToDefaultState)
                {
                    variableSave = elementContainingState.DefaultState.GetVariableSave(variableName);
                    if (variableSave == null)
                    {
                        shouldGoToBaseType = canGoToBase;
                    }
                }

                if (shouldGoToBaseType)
                {
                    var baseElement = ObjectFinder.Self.GetElementSave(elementContainingState.BaseType);

                    if (baseElement != null)
                    {
                        variableSave = baseElement.DefaultState.GetVariableRecursive(variableName);
                    }
                }
                else if (shouldGoToInstanceComponent)
                {
                    ElementSave instanceElement = null;
                    if (instance != null)
                    {
                        instanceElement = ObjectFinder.Self.GetElementSave(instance);
                    }

                    if (instanceElement != null)
                    {
                        variableSave = instanceElement.DefaultState.GetVariableRecursive(VariableSave.GetRootName(variableName));
                    }
                }

            }
        }

        return variableSave;
    }

    public static VariableListSave GetVariableListRecursive(this StateSave stateSave, string variableName)
    {
        VariableListSave variableListSave = stateSave.GetVariableListSave(variableName);

        if (variableListSave == null)
        {
            ElementSave elementContainingState = stateSave.ParentContainer;

            // 1. Go to the default state if it's not a default
            bool shouldGoToDefaultState = false;
            // 2. Go to the base type if the variable is on the container itself, or if the instance is DefinedByBase
            bool shouldGoToBaseType = false;
            // 3. Go to the instance if it's on an instance and we're not going to the default state or base type
            bool shouldGoToInstanceComponent = false;



            if (elementContainingState != null)
            {
                if (elementContainingState != null && stateSave != elementContainingState.DefaultState)
                {
                    shouldGoToDefaultState = true;
                }

                var isVariableOnInstance = variableName.Contains('.');
                InstanceSave instance = null;
                bool canGoToBase = false;


                var hasBaseType = !string.IsNullOrEmpty(elementContainingState.BaseType);
                var isVariableDefinedOnThisInheritanceLevel = false;

                var instanceName = VariableSave.GetSourceObject(variableName);
                instance = elementContainingState.Instances.FirstOrDefault(item => item.Name == instanceName);

                if (isVariableOnInstance && hasBaseType)
                {
                    if (instance != null && instance.DefinedByBase == false)
                    {
                        isVariableDefinedOnThisInheritanceLevel = true;
                    }
                }
                else if (!hasBaseType)
                {
                    isVariableDefinedOnThisInheritanceLevel = true;
                }

                canGoToBase = isVariableOnInstance == false ||
                    isVariableDefinedOnThisInheritanceLevel == false;

                if (!shouldGoToDefaultState)
                {
                    shouldGoToBaseType = canGoToBase;
                }

                if (!shouldGoToDefaultState && !shouldGoToBaseType)
                {
                    shouldGoToInstanceComponent = isVariableOnInstance;
                }



                if (shouldGoToDefaultState)
                {
                    variableListSave = elementContainingState.DefaultState.GetVariableListSave(variableName);
                    if (variableListSave == null)
                    {
                        shouldGoToBaseType = canGoToBase;
                    }
                }


                if (shouldGoToBaseType)
                {
                    var baseElement = ObjectFinder.Self.GetElementSave(elementContainingState.BaseType);

                    if (baseElement != null)
                    {
                        variableListSave = baseElement.DefaultState.GetVariableListRecursive(variableName);
                    }
                }
                else if (shouldGoToInstanceComponent)
                {
                    ElementSave instanceType = null;
                    if (instance != null)
                    {
                        instanceType = ObjectFinder.Self.GetElementSave(instance);
                    }

                    if (instanceType != null)
                    {
                        var nameInBase = VariableSave.GetRootName(variableName);
                        var wasFound = false;
                        for (int i = 0; i < stateSave.Variables.Count; i++)
                        //foreach (var instanceStateVariable in statesSetOnThisInstance)
                        {
                            var instanceStateVariable = stateSave.Variables[i];
                            if (instanceStateVariable.SourceObject == instanceName && instanceStateVariable.SetsValue &&
                                // check this last since it's the slowest:
                                instanceStateVariable.IsState(elementContainingState))
                            {
                                var matchingState = instanceType.AllStates.FirstOrDefault(item => item.Name == (string)instanceStateVariable.Value);

                                if (matchingState != null)
                                {
                                    variableListSave = matchingState.GetVariableListRecursive(nameInBase);
                                    wasFound = variableListSave != null;
                                    if (wasFound)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        if (!wasFound)
                        {
                            variableListSave = instanceType.DefaultState.GetVariableListRecursive(nameInBase);
                        }
                    }
                }
            }
        }
        return variableListSave;
    }




    /// <summary>
    /// Sets the value on the argument instance save, determining internally if it is a VariableSave or VariableListSave.
    /// </summary>
    /// <param name="stateSave">The StateSave which contains the variable, or which will be given the new variable.</param>
    /// <param name="variableName">The name of the variable</param>
    /// <param name="value">The value of the variable</param>
    /// <param name="instanceSave">The instance modified by the variable.</param>
    /// <param name="variableType">The type of the variable. If this is a VariableList, then the type of the items inside the list (like int)</param>
    public static void SetValue(this StateSave stateSave, string variableName, object value,
        InstanceSave? instanceSave = null, string? variableType = null)
    {
        bool isReservedName = TrySetReservedValues(stateSave, variableName, value, instanceSave);


        if (!isReservedName)
        {
            VariableSave variableSave = stateSave.GetVariableSave(variableName);
            var coreVariableDefinition = stateSave.GetVariableRecursive(variableName);
            VariableSave? rootVariable = null;
            var element = instanceSave?.ParentContainer ?? stateSave.ParentContainer;
            if (element != null)
            {
                rootVariable = ObjectFinder.Self.GetRootVariable(variableName, instanceSave?.ParentContainer ?? stateSave.ParentContainer);
            }

            string exposedVariableSourceName = null;
            if (!string.IsNullOrEmpty(coreVariableDefinition?.ExposedAsName) && instanceSave == null)
            {
                exposedVariableSourceName = coreVariableDefinition.Name;
            }
            bool isFile = DetermineIfIsFile(stateSave, variableName, instanceSave, variableSave);

            if (value != null && value is IList)
            {
                // This should already be the type
                // in the list like "int" and not "List<int>"
                string typeInList = variableType;

                stateSave.AssignVariableListSave(variableName, value, instanceSave, typeInList);
            }
            else
            {

                variableSave = stateSave.AssignVariableSave(variableName, value, instanceSave, variableType);

                variableSave.IsFont = rootVariable?.IsFont == true;
                variableSave.IsFile = isFile;

                if (!string.IsNullOrEmpty(exposedVariableSourceName))
                {
                    variableSave.ExposedAsName = variableName;
                    variableSave.Name = exposedVariableSourceName;
                }

                stateSave.Variables.Sort((first, second) => first.Name.CompareTo(second.Name));
            }




            if (isFile &&
                value is string asString &&
                !string.IsNullOrEmpty(asString) &&
                !FileManager.IsRelative(asString))
            {
                string directoryToMakeRelativeTo = FileManager.GetDirectory(ObjectFinder.Self.GumProjectSave.FullFileName);

                const bool preserveCase = true;
                if(!FileManager.IsUrl(asString))
                {
                    value = FileManager.MakeRelative(asString, directoryToMakeRelativeTo, preserveCase);
                }

                // re-assign the value using the relative name now
                var assignedVariable = stateSave.AssignVariableSave(variableName, value, instanceSave, variableType);
                assignedVariable.IsFile = isFile;
            }
        }

    }

    private static bool DetermineIfIsFile(StateSave stateSave, string variableName, InstanceSave instanceSave, VariableSave variableSave)
    {
        bool isFile = false;

        // Why might instanceSave be null?
        // The reason is because StateSaves
        // are used both for actual game data
        // as well as temporary variable containers.
        // If a StateSave is a temporary container then
        // instanceSave may (probably will be) null.
        if (instanceSave != null)
        {
            VariableSave temp = variableSave;
            if (variableSave == null)
            {
                temp = new VariableSave();
                temp.Name = variableName;
            }
            isFile = temp.GetIsFileFromRoot(instanceSave);
        }
        else
        {
            VariableSave temp = variableSave;
            if (variableSave == null)
            {
                temp = new VariableSave();
                temp.Name = variableName;
            }
            if (stateSave.ParentContainer != null)
            {
                isFile = temp.GetIsFileFromRoot(stateSave.ParentContainer);
            }
        }

        return isFile;
    }

    private static bool TrySetReservedValues(StateSave stateSave, string variableName, object value, InstanceSave instanceSave)
    {
        bool isReservedName = false;

        // Check for reserved names
        if (variableName == "Name")
        {
            stateSave.ParentContainer.Name = value as string;
            isReservedName = true;
        }
        else if (variableName == "BaseType")
        {
            if(value?.ToString() == string.Empty)
            {
                stateSave.ParentContainer.BaseType = null;
            }
            else
            {
                stateSave.ParentContainer.BaseType = value?.ToString();
            }
            isReservedName = true; // don't do anything
        }

        if (StringFunctions.ContainsNoAlloc(variableName, '.'))
        {
            string instanceName = variableName.Substring(0, variableName.IndexOf('.'));

            ElementSave elementSave = stateSave.ParentContainer;

            // This is a variable on an instance
            if (variableName.EndsWith(".Name"))
            {
                instanceSave.Name = (string)value;
                isReservedName = true;
            }
            else if (variableName.EndsWith(".BaseType"))
            {
                instanceSave.BaseType = value.ToString();
                isReservedName = true;
            }
            else if (variableName.EndsWith(".Locked"))
            {
                instanceSave.Locked = (bool)value;
                isReservedName = true;
            }
            else if (variableName.EndsWith(".IsSlot"))
            {
                instanceSave.IsSlot = (bool)value;
                isReservedName = true;
            }
        }
        return isReservedName;
    }


    private static void AssignVariableListSave(this StateSave stateSave, string variableName, object value, InstanceSave instanceSave, string? typeInList = null)
    {
        VariableListSave variableListSave = stateSave.GetVariableListSave(variableName);

        if (variableListSave == null)
        {
            if (value is List<string>)
            {
                variableListSave = new VariableListSave<string>();
            }
            else if (value is List<System.Numerics.Vector2>)
            {
                variableListSave = new VariableListSave<System.Numerics.Vector2>();
            }
            else
            {
                throw new NotImplementedException($"Error setting List value to type {value?.GetType()} - need to add explicit support for this type");
            }
            variableListSave.Type = typeInList ?? "string";

            variableListSave.Name = variableName;

            //if (instanceSave != null)
            //{
            //    variableListSave.SourceObject = instanceSave.Name;
            //}

            stateSave.VariableLists.Add(variableListSave);
        }

        // See comments in AssignVariableSave about why we do this outside of the above if-statement.

        if (StringFunctions.ContainsNoAlloc(variableName, '.'))
        {
            string rootName = variableListSave.Name.Substring(variableListSave.Name.IndexOf('.') + 1);

            string sourceObjectName = variableListSave.Name.Substring(0, variableListSave.Name.IndexOf('.'));

            if (instanceSave == null && stateSave.ParentContainer != null)
            {
                instanceSave = stateSave.ParentContainer.GetInstance(sourceObjectName);
            }
            if (instanceSave != null)
            {
                VariableListSave baseVariableListSave = ObjectFinder.Self.GetRootStandardElementSave(instanceSave).DefaultState.GetVariableListSave(rootName);
                variableListSave.IsFile = baseVariableListSave?.IsFile == true;
            }
            variableListSave.Name = variableName;
        }

        // Don't assign the actual reference. Doing so may result in an element instance 
        // sharing the same IList reference as the StandardElement:
        //variableListSave.ValueAsIList = value as IList;
        variableListSave.ValueAsIList.Clear();
        if(value as IList == null)
        {
            variableListSave.ValueAsIList = null;
        }
        else
        {
            foreach(var item in value as IList)
            {
                variableListSave.ValueAsIList.Add(item);
            }
        }
    }

    /// <summary>
    /// Assigns a value to a variable.  If the variable doesn't exist then the variable is instantiated, then the value is assigned.
    /// </summary>
    /// <param name="stateSave">The StateSave that contains the variable.  The variable will be added to this StateSave if it doesn't exist.</param>
    /// <param name="variableName">The name of the variable to look for.</param>
    /// <param name="value">The value to assign to the variable.</param>
    /// <param name="instanceSave">The instance that owns this variable.  This may be null.</param>
    /// <param name="variableType">The type of the variable.  This is only needed if the value is null.</param>
    private static VariableSave AssignVariableSave(this StateSave stateSave, string variableName, object value,
        InstanceSave instanceSave, string variableType = null)
    {
        // Not a reserved variable, so use the State's variables
        VariableSave variableSave = stateSave.GetVariableSave(variableName);

        if (variableSave == null)
        {
            variableSave = new VariableSave();

            // If the variableType is not null, give it priority
            if (!string.IsNullOrEmpty(variableType))
            {
                variableSave.Type = variableType;
            }

            else if (value is bool)
            {
                variableSave.Type = "bool";
            }
            else if (value is bool?)
            {
                variableSave.Type = "bool?";
            }
            else if (value is float)
            {
                variableSave.Type = "float";
            }
            else if (value is float?)
            {
                variableSave.Type = "float?";
            }
            else if (value is decimal)
            {
                variableSave.Type = "decimal";
            }
            else if (value is decimal?)
            {
                variableSave.Type = "decimal?";
            }
            else if (value is int)
            {
                variableSave.Type = "int";
            }
            else if (value is int?)
            {
                variableSave.Type = "int?";
            }
            // account for enums
            else if (value is string)
            {
                variableSave.Type = "string";
            }
            else if (value == null)
            {
                variableSave.Type = variableType;
            }
            else
            {
                variableSave.Type = value.GetType().ToString();
            }


            variableSave.Name = variableName;

            stateSave.Variables.Add(variableSave);
        }




        // There seems to be
        // two ways to indicate
        // that a variable has a
        // source object.  One is
        // to pass a InstanceSave to
        // this method, another is to
        // include a '.' in the name.  If
        // an instanceSave is passed, then
        // a dot MUST be present.  I don't think
        // we allow a dot to exist without a variable
        // representing a variable on an instance save,
        // so I'm not sure why we even require an InstanceSave.
        // Also, it seems like code (especially plugins) may not
        // know to pass an InstanceSave and may assume that the dot
        // is all that's needed.  If so, we shouldn't be strict and require
        // a non-null InstanceSave.  
        //if (instanceSave != null)
        // Update:  We used to only check this when first creating a Variable, but
        // there's no harm in forcing the source object.  Let's do that.
        // Update:  Turns out we do need the instance so that we can get the base type
        // to find out if the variable IsFile or not.  If the InstanceSave is null, but 
        // we have a sourceObjectName that we determine by the presence of a dot, then let's
        // try to find the InstanceSave
        if (StringFunctions.ContainsNoAlloc(variableName, '.'))
        {
            string rootName = variableSave.Name.Substring(variableSave.Name.IndexOf('.') + 1);
            string sourceObjectName = variableSave.Name.Substring(0, variableSave.Name.IndexOf('.'));

            if (instanceSave == null && stateSave.ParentContainer != null)
            {
                instanceSave = stateSave.ParentContainer.GetInstance(sourceObjectName);
            }

            //ElementSave baseElement = ObjectFinder.Self.GetRootStandardElementSave(instanceSave);

            //VariableSave baseVariableSave = baseElement.DefaultState.GetVariableSave(rootName);
            if (instanceSave != null)
            {
                // can we get this from the base element?
                var instanceBase = ObjectFinder.Self.GetElementSave(instanceSave);
                bool found = false;

                if (instanceBase != null)
                {
                    VariableSave baseVariableSave = instanceBase.DefaultState.Variables.FirstOrDefault(item => item.ExposedAsName == rootName || item.Name == rootName);
                    if (baseVariableSave != null)
                    {
                        variableSave.IsFile = baseVariableSave.IsFile;
                        found = true;
                    }
                }

                if (!found)
                {
                    VariableSave baseVariableSave = ObjectFinder.Self.GetRootStandardElementSave(instanceSave).DefaultState.GetVariableSave(rootName);
                    if (baseVariableSave != null)
                    {
                        variableSave.IsFile = baseVariableSave.IsFile;
                    }
                }

            }
        }

        variableSave.SetsValue = true;

        variableSave.Value = value;
        if(!string.IsNullOrEmpty(variableType))
        {
            variableSave.Type = variableType;
        }
        return variableSave;
    }

    public static StateSave Clone(this StateSave whatToClone)
    {
        return whatToClone.Clone<StateSave>();

    }

    public static T Clone<T>(this StateSave whatToClone) where T : StateSave
    {
        T toReturn = FileManager.CloneSaveObjectCast<StateSave, T>(whatToClone);

        toReturn.Variables.Clear();
        foreach (VariableSave vs in whatToClone.Variables)
        {
            toReturn.Variables.Add(vs.Clone());
        }



        // do we also want to clone VariableSaveLists?  Not sure at this point

        return toReturn;

    }



    public static void ConvertEnumerationValuesToInts(this StateSave stateSave)
    {
        foreach (VariableSave variable in stateSave.Variables)
        {
            variable.ConvertEnumerationValuesToInts();
        }
    }

    public static void FixEnumerations(this StateSave stateSave)
    {
        foreach (VariableSave variable in stateSave.Variables)
        {
            variable.FixEnumerations();
        }

        // Do w want to fix enums here?
        //foreach (VariableListSave variableList in otherStateSave.VariableLists)
        //{
        //    stateSave.VariableLists.Add(FileManager.CloneSaveObject(variableList));
        //}


    }
    // I wrote this for animation but it turns out it isn't going to work how I expected
    //public static StateSave CombineBaseValuesAndClone(this StateSave stateSave)
    //{
    //    StateSave cloned = new StateSave();

    //    if (stateSave.ParentContainer == null)
    //    {
    //        // This thing doesn't have a parent container so we have no idea how to get the default and follow inheritance
    //        cloned = stateSave.Clone();
    //    }
    //    else
    //    {
    //        ElementSave parent = stateSave.ParentContainer;
    //        if (parent.DefaultState == stateSave)
    //        {
    //            if (string.IsNullOrEmpty(parent.BaseType))
    //            {
    //                cloned = stateSave.Clone();
    //            }
    //            else
    //            {
    //                ElementSave baseOfParent = ObjectFinder.Self.GetElementSave(parent.BaseType);

    //                if (baseOfParent == null)
    //                {
    //                    cloned = stateSave.Clone();
    //                }
    //                else
    //                {
    //                    cloned = baseOfParent.DefaultState.CombineBaseValuesAndClone();

    //                    cloned.MergeIntoThis(stateSave);

    //                }
    //            }
    //        }
    //        else
    //        {
    //            cloned = parent.DefaultState.CombineBaseValuesAndClone();

    //            cloned.MergeIntoThis(stateSave);
    //        }
    //    }


    //    return cloned;

    //}

    /// <summary>
    /// Merges two states into a list of VariableSaveValues. This is an efficient way to perform state interpolation.
    /// </summary>
    /// <param name="firstState">The first state.</param>
    /// <param name="secondState">The second state.</param>
    /// <param name="secondRatio">The ratio of the second state. This value should be between 0 and 1.</param>
    /// <param name="mergedValues">The resulting values.</param>
    /// <exception cref="ArgumentNullException">If either of the argument states are null.</exception>
    public static void Merge(StateSave firstState, StateSave secondState, float secondRatio, List<VariableSaveValues> mergedValues)
    {
#if FULL_DIAGNOSTICS
        if (firstState == null || secondState == null)
        {
            throw new ArgumentNullException("States must not be null");
        }
#endif

        foreach (var secondVariable in secondState.Variables)
        {
            object secondValue = secondVariable.Value;

            VariableSave firstVariable = firstState.GetVariableSave(secondVariable.Name);

            // If this variable doesn't have a value, or if the variable doesn't set the variable
            // then we need to go recursive to see what the value is:
            bool needsValueFromBase = firstVariable == null || firstVariable.SetsValue == false;
            bool setsValue = secondVariable.SetsValue;

            object firstValue = null;

            if (firstVariable == null)
            {
                firstValue = secondVariable.Value;

                // Get the value recursively before adding it to the list
                if (needsValueFromBase)
                {
                    var variableOnThis = firstState.GetVariableSave(secondVariable.Name);
                    if (variableOnThis != null)
                    {
                        setsValue |= variableOnThis.SetsValue;
                    }

                    firstValue = firstState.GetValueRecursive(secondVariable.Name);
                }
            }
            else
            {
                firstValue = firstVariable.Value;
            }

            if (setsValue)
            {
                object interpolated = GetValueConsideringInterpolation(firstValue, secondValue, secondRatio);

                VariableSaveValues value = new VariableSaveValues();
                value.Name = secondVariable.Name;
                value.Value = interpolated;

                mergedValues.Add(value);
            }
        }

        // todo:  Handle lists?



    }

    public static void MergeIntoThis(this StateSave thisState, StateSave other, float otherRatio = 1)
    {
#if FULL_DIAGNOSTICS
        if (other == null)
        {
            throw new ArgumentNullException("other Statesave is null and it shouldn't be");
        }
#endif

        foreach (var variableSave in other.Variables)
        {
            // The first will use its default if one doesn't exist
            VariableSave whatToSet = thisState.GetVariableSave(variableSave.Name);

            // If this variable doesn't have a value, or if the variable doesn't set the variable
            // then we need to go recursive to see what the value is:
            bool needsValueFromBase = whatToSet == null || whatToSet.SetsValue == false;
            bool setsValue = variableSave.SetsValue;


            if (whatToSet == null)
            {
                whatToSet = variableSave.Clone();

                // Get the value recursively before adding it to the list
                if (needsValueFromBase)
                {
                    var variableOnThis = thisState.GetVariableSave(variableSave.Name);
                    if (variableOnThis != null)
                    {
                        setsValue |= variableOnThis.SetsValue;
                    }
                    whatToSet.Value = thisState.GetValueRecursive(variableSave.Name);
                }

                thisState.Variables.Add(whatToSet);
            }


            whatToSet.SetsValue = setsValue;
            whatToSet.Value = GetValueConsideringInterpolation(whatToSet.Value, variableSave.Value, otherRatio);
        }

        // todo:  Handle lists?

    }

    public static void AddIntoThis(this StateSave thisState, StateSave other)
    {
        foreach (var variableSave in other.Variables)
        {
            // The first will use its default if one doesn't exist
            VariableSave whatToSet = thisState.GetVariableSave(variableSave.Name);

            if (whatToSet != null && (whatToSet.SetsValue || variableSave.SetsValue))
            {
                whatToSet.SetsValue = true;
                whatToSet.Value = AddValue(whatToSet, variableSave);

            }
        }

        // todo:  Handle lists?

    }

    public static void SubtractFromThis(this StateSave thisState, StateSave other)
    {
        foreach (var variableSave in other.Variables)
        {
            // The first will use its default if one doesn't exist
            VariableSave whatToSet = thisState.GetVariableSave(variableSave.Name);

            if (whatToSet != null && (whatToSet.SetsValue || variableSave.SetsValue))
            {
                whatToSet.SetsValue = true;
                whatToSet.Value = SubtractValue(whatToSet, variableSave);

            }
        }

        // todo:  Handle lists?

    }

    private static object SubtractValue(VariableSave firstVariable, VariableSave secondVariable)
    {
        if (firstVariable.Value == null || secondVariable.Value == null)
        {
            return secondVariable.Value;
        }
        else if (firstVariable.Value is float && secondVariable.Value is float)
        {
            float firstFloat = (float)firstVariable.Value;
            float secondFloat = (float)secondVariable.Value;

            return firstFloat - secondFloat;
        }
        else if (firstVariable.Value is double && secondVariable.Value is double)
        {
            double firstDouble = (double)firstVariable.Value;
            double secondDouble = (double)secondVariable.Value;

            return firstDouble - secondDouble;
        }

        else if (firstVariable.Value is int)
        {
            int firstInt = (int)firstVariable.Value;
            int secondInt = (int)secondVariable.Value;

            return firstInt - secondInt;
        }
        else
        {
            return secondVariable.Value;
        }

    }



    private static object AddValue(VariableSave firstVariable, VariableSave secondVariable)
    {
        if (firstVariable.Value == null || secondVariable.Value == null)
        {
            return secondVariable.Value;
        }
        else if (firstVariable.Value is float && secondVariable.Value is float)
        {
            float firstFloat = (float)firstVariable.Value;
            float secondFloat = (float)secondVariable.Value;

            return firstFloat + secondFloat;
        }
        else if (firstVariable.Value is double && secondVariable.Value is double)
        {
            double firstDouble = (double)firstVariable.Value;
            double secondDouble = (double)secondVariable.Value;

            return firstDouble + secondDouble;
        }

        else if (firstVariable.Value is int)
        {
            int firstInt = (int)firstVariable.Value;
            int secondInt = (int)secondVariable.Value;

            return firstInt + secondInt;
        }
        else
        {
            return secondVariable.Value;
        }

    }

    /// <summary>
    /// Returns a value that is the interpolation between the first and second values if the value is cast as an object. The value must ultimately be a numeric value.
    /// </summary>
    /// <param name="firstValue">The first value as a numeric value.</param>
    /// <param name="secondValue">The second value as a numeric value.</param>
    /// <param name="interpolationValue">A value between 0 and 1. A value of 0 returns the firstValue. A value of 1 returns the second value.</param>
    /// <returns>The resulting interpolated value, matching the type of the arguments.</returns>
    public static object GetValueConsideringInterpolation(object firstValue, object secondValue, float interpolationValue)
    {
        if (firstValue == null || secondValue == null)
        {
            return secondValue;
        }
        else if (firstValue is float && secondValue is float)
        {
            float firstFloat = (float)firstValue;
            float secondFloat = (float)secondValue;

            return firstFloat + (secondFloat - firstFloat) * interpolationValue;
        }
        else if (firstValue is double && secondValue is double)
        {
            double firstFloat = (double)firstValue;
            double secondFloat = (double)secondValue;

            return firstFloat + (secondFloat - firstFloat) * interpolationValue;
        }

        else if (firstValue is int)
        {
            int firstFloat = (int)firstValue;
            int secondFloat = (int)secondValue;

            return (int)(.5f + firstFloat + (secondFloat - firstFloat) * interpolationValue);
        }
        if(firstValue is bool && secondValue is bool)
        {
            return interpolationValue >= 1 ? secondValue : firstValue;
        }
        else
        {
            return secondValue;
        }
    }



    public static void RemoveValue(this StateSave stateSave, string variableName)
    {
        var variableSave = stateSave.GetVariableSave(variableName);
        if (variableSave != null)
        {
            stateSave.Variables.Remove(variableSave);
        }
    }
}
