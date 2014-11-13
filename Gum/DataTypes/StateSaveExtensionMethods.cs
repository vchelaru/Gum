using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System.Collections;
using ToolsUtilities;
using Gum.Wireframe;

//using Gum.Reflection;


namespace Gum.DataTypes.Variables
{
    public static class StateSaveExtensionMethods
    {
        public static void Initialize(this StateSave stateSave)
        {
            foreach (VariableSave variable in stateSave.Variables)
            {
                variable.FixEnumerations();
            }
            stateSave.Variables.Sort((a, b) => a.Name.CompareTo(b.Name));
        }

        public static object GetValueRecursive(this StateSave stateSave, string variableName)
        {
            object value = stateSave.GetValue(variableName);

            if (value == null)
            {
                // Is this thing the default?
                ElementSave parent = stateSave.ParentContainer;

                // I don't know if we need this code
                // because if we got in here, then the non-default failed to find a value
                // Update July 12, 2013
                // Not sure why I commented this code out.  This code lets us check a non-default
                // state, and if it doesn't contain a value, then we look at the default state in this 
                // element.  Then if that fails, we can climb up the inheritance tree.
                // Let's see if we can get something from the non-default first
                bool wasFound = false;
                if (parent != null && stateSave != parent.DefaultState)
                {
                    // try to get it from the stateSave
                    var foundVariable = stateSave.GetVariableRecursive(variableName);
                    if (foundVariable != null && foundVariable.SetsValue)
                    {
                        // Why do we early out here?
                        //return foundVariable.Value;
                        value = foundVariable.Value;
                        wasFound = true;
                    }
                }
                
                if (!wasFound && parent != null)
                {
                    ElementSave baseElement = GetBaseElementFromVariable(variableName, parent);
                    
                    if (baseElement != null)
                    {
                        string nameInBase = variableName;

                        if (variableName.Contains('.'))
                        {
                            // this variable is set on an instance, but we're going into the
                            // base type, so we want to get the raw variable and not the variable
                            // as tied to an instance.
                            nameInBase = variableName.Substring(nameInBase.IndexOf('.') + 1);
                        }

                        value = baseElement.DefaultState.GetValueRecursive(nameInBase);


                    }


                    if (value == null && parent is ComponentSave)
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


        public static VariableSave GetVariableRecursive(this StateSave stateSave, string variableName)
        {
            VariableSave variableSave = stateSave.GetVariableSave(variableName);

            if (variableSave == null)
            {
                // Is this thing the default?
                ElementSave parent = stateSave.ParentContainer;

                if (parent != null && stateSave != parent.DefaultState)
                {
                    variableSave = stateSave.GetVariableSave(variableName);

                    if (variableSave == null)
                    {
                        variableSave = parent.DefaultState.GetVariableSave(variableName);
                    }
                }
                
                if (variableSave == null && parent != null)
                {
                    ElementSave baseElement = GetBaseElementFromVariable(variableName, parent);

                    if (baseElement != null)
                    {
                        string nameInBase = variableName;

                        if (variableName.Contains('.'))
                        {
                            // this variable is set on an instance, but we're going into the
                            // base type, so we want to get the raw variable and not the variable
                            // as tied to an instance.
                            nameInBase = variableName.Substring(nameInBase.IndexOf('.') + 1);
                        }

                        return baseElement.DefaultState.GetVariableRecursive(nameInBase);
                    }
                }
                    
                return variableSave;
            }
            else
            {
                return variableSave;
            }
        }

        public static VariableListSave GetVariableListRecursive(this StateSave stateSave, string variableName)
        {
            VariableListSave variableListSave = stateSave.GetVariableListSave(variableName);

            if (variableListSave == null)
            {
                // Is this thing the default?
                ElementSave parent = stateSave.ParentContainer;

                if (parent != null && stateSave != parent.DefaultState)
                {
                    throw new NotImplementedException();
                }
                else if (parent != null)
                {
                    ElementSave baseElement = GetBaseElementFromVariable(variableName, parent);

                    if (baseElement != null)
                    {
                        string nameInBase = variableName;

                        if (variableName.Contains('.'))
                        {
                            // this variable is set on an instance, but we're going into the
                            // base type, so we want to get the raw variable and not the variable
                            // as tied to an instance.
                            nameInBase = variableName.Substring(nameInBase.IndexOf('.') + 1);
                        }

                        return baseElement.DefaultState.GetVariableListRecursive(nameInBase);
                    }
                }

                return null;
            }
            else
            {
                return variableListSave;
            }
        }


        public static void ReactToInstanceNameChange(this StateSave stateSave, string oldName, string newName)
        {
            foreach (VariableSave variable in stateSave.Variables)
            {
                if(variable.SourceObject == oldName)
                {
                    variable.Name = newName + "." +  variable.Name.Substring((oldName + ".").Length);
                }

                if (variable.GetRootName() == "Parent" && variable.SetsValue && variable.Value is string && 
                    (string)(variable.Value) == oldName)
                {
                    variable.Value = newName;
                }
            }

            foreach (VariableListSave variableList in stateSave.VariableLists)
            {
                if (variableList.SourceObject == oldName)
                {
                    if (variableList.SourceObject == oldName)
                    {
                        variableList.Name = newName + "." + variableList.Name.Substring((oldName + ".").Length);
                        variableList.SourceObject = newName;
                    }
                }
            }
        }


        public static void SetValue(this StateSave stateSave, string variableName, object value, InstanceSave instanceSave = null, string variableType = null)
        {
            bool isReservedName = TrySetReservedValues(stateSave, variableName, value, instanceSave);

            VariableSave variableSave = stateSave.GetVariableSave(variableName);

            string exposedVariableSourceName = null;

            string rootName = variableName;
            if (variableName.Contains('.'))
            {
                rootName = variableName.Substring(variableName.IndexOf('.') + 1);
            }
            else if(stateSave.ParentContainer != null && stateSave.ParentContainer.DefaultState != stateSave)
            {
                // This isn't the default state, so let's ask the default state if this is an exposed variable...
                var defaultState = stateSave.ParentContainer.DefaultState;

                var found = defaultState.Variables.FirstOrDefault(item=>item.ExposedAsName == variableName);
                if(found != null)
                {
                    exposedVariableSourceName = found.Name;
                }
            }

            if (!isReservedName)
            {
                if (value != null && value is IList)
                {
                    stateSave.AssignVariableListSave(variableName, value, instanceSave);
                }
                else
                {

                    variableSave = stateSave.AssignVariableSave(variableName, value, instanceSave, variableType);

                    if (!string.IsNullOrEmpty(exposedVariableSourceName))
                    {
                        variableSave.ExposedAsName = variableName;
                        variableSave.Name = exposedVariableSourceName;
                    }

                    stateSave.Variables.Sort((first, second) => first.Name.CompareTo(second.Name));
                }


                bool isFile = false;

                // Why might instanceSave be null?
                // The reason is because StateSaves
                // are used both for actual game data
                // as well as temporary variable containers.
                // If a StateSave is a temporary container then
                // instanceSave may (probably will be) null.
                if (instanceSave != null)
                {
                    isFile = variableSave.GetIsFileFromRoot(instanceSave);
                }
                else if (variableSave != null)
                {
                    isFile = variableSave.IsFile;
                }

                if (isFile &&
                    value is string &&
                    !FileManager.IsRelative((string)value))
                {
                    string directoryToMakeRelativeTo = FileManager.GetDirectory(ObjectFinder.Self.GumProjectSave.FullFileName);

                    const bool preserveCase = true;
                    value = FileManager.MakeRelative((string)value, directoryToMakeRelativeTo, preserveCase);

                    // re-assign the value using the relative name now
                    stateSave.AssignVariableSave(variableName, value, instanceSave, variableType);
                }
            }

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
            else if (variableName == "Base Type")
            {
                stateSave.ParentContainer.BaseType = value.ToString();
                isReservedName = true; // don't do anything
            }

            if (variableName.Contains('.'))
            {
                string instanceName = variableName.Substring(0, variableName.IndexOf('.'));

                ElementSave elementSave = stateSave.ParentContainer;

                // This is a variable on an instance
                if (variableName.EndsWith(".Name"))
                {
                    instanceSave.Name = (string)value;
                    isReservedName = true;
                }
                else if (variableName.EndsWith(".Base Type"))
                {
                    instanceSave.BaseType = value.ToString();
                    isReservedName = true;
                }
                else if (variableName.EndsWith(".Locked"))
                {
                    instanceSave.Locked = (bool)value;
                    isReservedName = true;
                }
            }
            return isReservedName;
        }


        private static void AssignVariableListSave(this StateSave stateSave, string variableName, object value, InstanceSave instanceSave)
        {
            VariableListSave variableListSave = stateSave.GetVariableListSave(variableName);

            if (variableListSave == null)
            {
                if (value is List<string>)
                {
                    variableListSave = new VariableListSave<string>();
                }
                variableListSave.Type = "string";

                variableListSave.Name = variableName;

                //if (instanceSave != null)
                //{
                //    variableListSave.SourceObject = instanceSave.Name;
                //}

                stateSave.VariableLists.Add(variableListSave);
            }

            // See comments in AssignVariableSave about why we do this outside of the above if-statement.

            if (variableName.Contains('.'))
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
                    variableListSave.IsFile = baseVariableListSave.IsFile;
                }
                variableListSave.SourceObject = sourceObjectName;
            }

            variableListSave.ValueAsIList = value as IList;
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
                if(!string.IsNullOrEmpty(variableType))
                {
                    variableSave.Type = variableType;
                }

                else if (value is bool)
                {
                    variableSave.Type = "bool";
                }
                else if (value is float)
                {
                    variableSave.Type = "float";
                }
                else if (value is int)
                {
                    variableSave.Type = "int";
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

                // Let's get the variable that this comes from to see if it's a file

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
            if (variableName.Contains('.'))
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

                    if(instanceBase != null)
                    {
                        VariableSave baseVariableSave = instanceBase.DefaultState.Variables.FirstOrDefault(item => item.ExposedAsName == rootName || item.Name == rootName);
                        if(baseVariableSave != null)
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

        public static void SetFrom(this StateSave stateSave, StateSave otherStateSave)
        {
            stateSave.Name = otherStateSave.Name;
            // We don't want to do this because the otherStateSave may not have a parent
            //stateSave.ParentContainer = otherStateSave.ParentContainer;

            stateSave.Variables.Clear();
            stateSave.VariableLists.Clear();

            foreach (VariableSave variable in otherStateSave.Variables)
            {
                stateSave.Variables.Add(FileManager.CloneSaveObject(variable));
            }

            foreach (VariableListSave variableList in otherStateSave.VariableLists)
            {
                stateSave.VariableLists.Add(FileManager.CloneSaveObject(variableList));
            }

#if GUM

            stateSave.FixEnumerations();
#endif
        }

#if GUM
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
#endif
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

        public static void Merge(StateSave firstState, StateSave secondState, float otherRatio, List<VariableSaveValues> mergedValues)
        {
#if DEBUG
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

                if (firstVariable != null)
                {
                    firstValue = firstVariable.Value;
                }

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

                if(setsValue)
                {
                    object interpolated = GetValueConsideringInterpolation(firstValue, secondValue, otherRatio);

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
#if DEBUG
            if(other == null)
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
                        if(variableOnThis != null)
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

                if(whatToSet != null && (whatToSet.SetsValue || variableSave.SetsValue))
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

        private static object GetValueConsideringInterpolation(object firstValue, object secondValue, float interpolationValue)
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
            else
            {
                return secondValue;
            }
        }
    }
}
