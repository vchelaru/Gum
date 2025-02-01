using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
#if !NO_XNA
#endif
using System;
using System.Collections.Generic;
using System.Linq;
#if GUM
using DynamicExpresso;
using Gum.PropertyGridHelpers;
using Xceed.Wpf.Toolkit.PropertyGrid.Converters;
#endif
using Gum.DataTypes.Variables;
using Gum.Managers;
using System.ComponentModel;
using System.Text;

namespace GumRuntime
{
    public static class ElementSaveExtensions
    {
        static Dictionary<string, Type> mElementToGueTypes = new Dictionary<string, Type>();
        static Dictionary<string, Func<GraphicalUiElement>> mElementToGueTypeFuncs = new Dictionary<string, Func<GraphicalUiElement>>();
        static Func<GraphicalUiElement> TemplateFunc;

        public static Func<StateSave, string, string, object> CustomEvaluateExpression;

        public static void RegisterGueInstantiationType(string elementName, Type gueInheritingType)
        {
            mElementToGueTypes[elementName] = gueInheritingType;
        }

        public static void RegisterGueInstantiation<T>(string elementName, Func<T> templateFunc) where T : GraphicalUiElement
        {
            mElementToGueTypeFuncs[elementName] = templateFunc;
        }


        public static void RegisterDefaultInstantiationType<T>(Func<T> templateFunc) where T : GraphicalUiElement
        {
            TemplateFunc = templateFunc;
        }



        public static GraphicalUiElement CreateGueForElement(ElementSave elementSave, bool fullInstantiation = false, string genericType = null)
        {
#if DEBUG
            if (elementSave == null)
            {
                throw new ArgumentNullException(nameof(elementSave));

            }
#endif
            GraphicalUiElement toReturn = null;

            var elementName = elementSave.Name;
            if (!string.IsNullOrEmpty(genericType))
            {
                elementName = elementName + "<T>";
            }

            if (mElementToGueTypeFuncs.ContainsKey(elementName))
            {
                toReturn = mElementToGueTypeFuncs[elementName]();
            }
            else if (mElementToGueTypes.ContainsKey(elementName))
            {
                // This code allows sytems (like games that use Gum) to assign types
                // to their GraphicalUiElements so that users of the code can work with
                // strongly-typed Gum objects.
                var type = mElementToGueTypes[elementName];

                if (!string.IsNullOrEmpty(genericType))
                {
                    type = type.MakeGenericType(mElementToGueTypes[genericType]);
                }
                var constructorWithArgs = type.GetConstructor(new Type[] { typeof(bool), typeof(bool) });
                if (constructorWithArgs != null)
                {
                    toReturn = constructorWithArgs.Invoke(new object[] { fullInstantiation, true }) as GraphicalUiElement;
                }
                else
                {
                    // For InteractiveGue in MonoGame Gum
                    toReturn = (GraphicalUiElement)Activator.CreateInstance(type);
                }
            }
            else if (TemplateFunc != null)
            {
                toReturn = TemplateFunc();
            }
            else
            {
                toReturn = new GraphicalUiElement();
            }
            toReturn.ElementSave = elementSave;
            return toReturn;
        }

        public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, ISystemManagers systemManagers,
            bool addToManagers, string genericType = null)
        {
            GraphicalUiElement toReturn = CreateGueForElement(elementSave, genericType: genericType);

            toReturn.Name = elementSave.Name;

            elementSave.SetGraphicalUiElement(toReturn, systemManagers);

            //no layering support yet
            if (addToManagers)
            {
                toReturn.AddToManagers(systemManagers, null);
            }

            return toReturn;
        }

        [Obsolete("Use AddStatesAndCategoriesRecursivelyToGue since that more clearly indicates what the method is doing")]
        public static void SetStatesAndCategoriesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave) => AddStatesAndCategoriesRecursivelyToGue(graphicalElement, elementSave);

        public static void AddStatesAndCategoriesRecursivelyToGue(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        {
            if (graphicalElement == null)
            {
                throw new ArgumentNullException(nameof(graphicalElement));
            }
            if (!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if (baseElementSave != null)
                {
                    graphicalElement.AddStatesAndCategoriesRecursivelyToGue(baseElementSave);
                }
            }

            // We need to set categories and states before calling SetGraphicalUiElement so that the states can be used
            foreach (var category in elementSave.Categories)
            {
                graphicalElement.AddCategory(category);
            }

            graphicalElement.AddStates(elementSave.States);
        }

        public static Func<string, ISystemManagers, IRenderable> CustomCreateGraphicalComponentFunc { get; set; }

        public static void CreateGraphicalComponent(this GraphicalUiElement graphicalElement, ElementSave elementSave, ISystemManagers systemManagers)
        {
            if (CustomCreateGraphicalComponentFunc == null)
            {
                throw new InvalidOperationException("The CustomCreateGraphicalComponentFunc must be set before calling CreateGraphicalComponent");
            }

            var containedObject = CustomCreateGraphicalComponentFunc(elementSave.Name, systemManagers);

            if (containedObject != null)
            {
                graphicalElement.SetContainedObject(containedObject);
            }
            else if (containedObject == null && !string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElement = ObjectFinder.Self.GetElementSave(elementSave.BaseType);

                if (baseElement != null)
                {
                    CreateGraphicalComponent(graphicalElement, baseElement, systemManagers);
                }
            }
        }

        public static void AddExposedVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        {
            if (!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if (baseElementSave != null)
                {
                    graphicalElement.AddExposedVariablesRecursively(baseElementSave);
                }
            }


            if (elementSave != null)
            {
                foreach (var variable in elementSave.DefaultState.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
                {
                    graphicalElement.AddExposedVariable(variable.ExposedAsName, variable.Name);
                }
            }

        }

        public static void SetVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave, Gum.DataTypes.Variables.StateSave stateSave)
        {
#if DEBUG
            if (stateSave == null)
            {
                throw new Exception("State cannot be null");
            }
#endif
            if (!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if (baseElementSave != null)
                {

                    graphicalElement.SetVariablesRecursively(baseElementSave, baseElementSave.DefaultState);
                }
            }

            graphicalElement.ApplyState(stateSave);

            ApplyVariableReferences(graphicalElement, stateSave);
        }

        public static bool ValueEquality(object val1, object val2)
        {
            if (val1 is string string1 && val2 is string string2)
            {
                return string1 == string2;
            }
            else
            {
                return val1?.Equals(val2) == true;
            }
        }

        public static void ApplyVariableReferences(this ElementSave element, StateSave stateSave)
        {
            foreach (var variableList in stateSave.VariableLists)
            {
                if (variableList.GetRootName() == "VariableReferences" && variableList.ValueAsIList.Count > 0)
                {
                    InstanceSave instance = null;
                    if (!string.IsNullOrEmpty(variableList.SourceObject))
                    {
                        instance = element.GetInstance(variableList.SourceObject);
                    }


                    foreach (string referenceString in variableList.ValueAsIList)
                    {
                        // this applies the variable and returns info about the application:
                        var result = ApplyVariableReferencesOnSpecificOwner(instance, referenceString, stateSave);
#if GUM
                        // In the gum tool, we need to check if the applicatoin actually changed the value
                        // If so, we notify plugins that the variable was changed in case any additional changes
                        // need to happen
                        if (!string.IsNullOrEmpty(result.VariableName))
                        {
                            var unqualified = result.VariableName;
                            if (unqualified?.Contains(".") == true)
                            {
                                unqualified = unqualified.Substring(unqualified.IndexOf(".") + 1);
                            }

                            if (!ValueEquality(result.OldValue, result.NewValue))
                            {
                                Gum.Plugins.PluginManager.Self.VariableSet(element, null, unqualified, result.OldValue);
                            }

                        }
#endif
                    }
                }
            }
        }

        /// <summary>
        /// Loops through all variable references in the argument graphicalUiElement, evaluates them, then applies the evlauated value to the
        /// owner of each variable reference.
        /// </summary>
        /// <param name="graphicalElement">The top level owner for which to apply variables.</param>
        /// <param name="stateSave">The current state, such as the argument graphicalElement's default state</param>
        public static void ApplyVariableReferences(this GraphicalUiElement graphicalElement, StateSave stateSave)
        {
            foreach (var variableList in stateSave.VariableLists)
            {
                if (variableList.GetRootName() == "VariableReferences" && variableList.ValueAsIList.Count > 0)
                {
                    if (variableList.SourceObject == null)
                    {
                        foreach (string referenceString in variableList.ValueAsIList)
                        {
                            ApplyVariableReferencesOnSpecificOwner(graphicalElement, referenceString, stateSave);
                        }
                    }
                    else
                    {
                        GraphicalUiElement instance = null;

                        if (graphicalElement.Tag is InstanceSave asInstanceSave && asInstanceSave.Name == variableList.SourceObject)
                        {
                            instance = graphicalElement;
                        }
                        else
                        {
                            // Give preferential treatment to the children of graphicalElement. If none are found, then go to the managers
                            // 
                            instance = graphicalElement.GetGraphicalUiElementByName(variableList.SourceObject);
                        }

                        if (instance != null)
                        {
                            foreach (string referenceString in variableList.ValueAsIList)
                            {
                                ApplyVariableReferencesOnSpecificOwner(instance, referenceString, stateSave);
                            }
                        }
                    }
                }
            }
        }

        static char[] equalsArray = new char[] { '=' };

        /// <summary>
        /// Evaluates the reference string (such as X = SomeOtherItem.X), applying the right side to the left side.
        /// </summary>
        /// <param name="referenceOwner">The owner that owns the variable reference, such as the instance.</param>
        /// <param name="referenceString">The string such as "X = SomeItem.X"</param>
        /// <param name="stateSave">The state save which owns the variable reference.</param>
        public static void ApplyVariableReferencesOnSpecificOwner(GraphicalUiElement referenceOwner, string referenceString, StateSave stateSave)
        {
            //////////////////////////////Early Out/////////////////////////////////
            if(referenceString?.StartsWith("//") == true)
            {
                return;
            }
            ////////////////////////////End Early Out///////////////////////////////
            
            // splits the left and right side, so that we get two items. the first is the left side like "X" and the second is the right side like "SomeItem.X"
            var split = referenceString
                .Split(equalsArray, 2, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim()).ToArray();

            if (split.Length != 2)
            {
                return;
            }

            var left = split[0];

            var instanceLeft = referenceOwner.Tag as InstanceSave;

            var leftVariableName = instanceLeft == null
                ? left
                : instanceLeft.Name + "." + left;

            string leftSideType = null;
            var leftVariableOnState = stateSave.Variables.FirstOrDefault(item => item.Name == leftVariableName);
            leftSideType = leftVariableOnState?.Type;
            if (leftSideType == null)
            {
                var elementOwningInstance = instanceLeft?.ParentContainer ?? stateSave.ParentContainer;
                leftSideType = ObjectFinder.Self.GetRootVariable(leftVariableName, elementOwningInstance)?.Type;
            }


            var right = split[1];
            var value = GetRightSideValue(stateSave, right, leftSideType);


            if (value != null)
            {
                referenceOwner.SetProperty(left, value);
            }
        }

#if GUM
        private static object ApplyVariablesUsingInterpreter(string[] split, string left, InstanceSave instanceLeft, object value)
        {
            var currentScreenOrComponent = ObjectFinder.Self.GetElementContainerOf(instanceLeft);
            // We used to use an interpreter. The problem with the interpreter is it doesn't
            // understand the recursive variable finder, so it only applies explicitly set variales
            var interpreter = new Interpreter(InterpreterOptions.PrimitiveTypes | InterpreterOptions.SystemKeywords);
            AddAllVariablesToInterpreter(currentScreenOrComponent, interpreter);


            // The interpreter has to treat each fully qualified variable as a separate value
            // The variable "MyObject.X" cannot be seen as a qualifying object with its variable.
            // Instead it has to be a standard C#
            // variable. Therefore, we replace all periods and slashes with \u1234.
            // Why \u1234? Not sure, need to ask arcnor, but I suspect it's a variable
            // that is valid for variables, but will not be used by users when writing scripts.
            // Also, there is some ambiguity in variable references. Currently the character '/' is used
            // to separate folders in an object, and the period is used to separate objects from their variables.
            // However, in the future we may want to support math operations which include decimals and division.
            // This would create ambiguity so we'd need to create a standard way to identify what is math vs. what
            // is gum variable references...
            // For now, no math operations are supported in variable references.
            string expression = split[1].Replace('.', '\u1234').Replace('/', '\u1234');
            try
            {
                var parsedExpression = interpreter.Parse(expression);
                value = parsedExpression.Invoke();

                var variableLeft = currentScreenOrComponent.DefaultState.GetVariableRecursive(instanceLeft.Name + "." + left);
                var variableLeftType = variableLeft.GetRuntimeType();

                value = Convert.ChangeType(value, variableLeftType);
            }
            catch (Exception)
            {
                // TODO: Show error
            }

            return value;
        }
#endif

#if GUM
        private static void AddAllVariablesToInterpreter(ElementSave currentScreenOrComponent, Interpreter interpreter)
        {
            foreach (var screen in ObjectFinder.Self.GumProjectSave.Screens)
            {
                AddVariablesToInterpreter(screen, "Screens");
            }

            foreach (var component in ObjectFinder.Self.GumProjectSave.Components)
            {
                AddVariablesToInterpreter(component, "Components");
            }

            void AddVariablesToInterpreter(ElementSave element, string screensOrComponents)
            {
                var allVariables = element.DefaultState.Variables;
                var prefix = $"{screensOrComponents}/" + element.Name + "/";
                AddVaraiblesWithPrefix(prefix, allVariables);
                // Also add unqualified versions:
                if (element == currentScreenOrComponent)
                {
                    AddVaraiblesWithPrefix("", allVariables);
                }
            }

            void AddVaraiblesWithPrefix(string prefix, List<VariableSave> allVariables)
            {
                foreach (var variable in allVariables)
                {
                    var vValue = variable.Value;
                    var name = variable.Name; // this would be something like ColoredRectangleInstance.X
                    var fullVariableName = (prefix + name).Replace('.', '\u1234').Replace('/', '\u1234');
                    interpreter.SetVariable(fullVariableName, vValue);
                }
            }
        }
#endif
        struct VariableReferenceAssignmentResult
        {
            public string VariableName;
            public object OldValue;
            public object NewValue;
        }

        private static VariableReferenceAssignmentResult ApplyVariableReferencesOnSpecificOwner(InstanceSave instanceLeft, string referenceString, StateSave stateSave)
        {

            //////////////////////////////Early Out/////////////////////////////////
            if (referenceString?.StartsWith("//") == true)
            {
                return new VariableReferenceAssignmentResult { NewValue = null, OldValue = null, VariableName = null };
            }
            ////////////////////////////End Early Out///////////////////////////////

            var split = referenceString
                .Split(equalsArray, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim()).ToArray();

            if (split.Length != 2)
            {
                return new VariableReferenceAssignmentResult { NewValue = null, OldValue = null, VariableName = null };
            }

            var left = split[0];

            var leftVariableName = instanceLeft == null
                ? left
                : instanceLeft.Name + "." + left;

            string leftSideType = null;
            var variableOnState = stateSave.Variables.FirstOrDefault(item =>  item.Name == leftVariableName);
            leftSideType = variableOnState?.Type;
            if(leftSideType == null)
            {
                var elementOwningInstance = instanceLeft?.ParentContainer ?? stateSave.ParentContainer;
                leftSideType = ObjectFinder.Self.GetRootVariable(leftVariableName, elementOwningInstance)?.Type;
            }

            var right = split[1];
            object value = GetRightSideValue(stateSave, right, leftSideType);

            object valueBefore = null;
            string effectiveLeft = null;
            if (value != null)
            {
                if (instanceLeft == null)
                {
                    effectiveLeft = left;
                }
                else
                {
                    effectiveLeft = $"{instanceLeft.Name}.{left}";
                }
                valueBefore = stateSave.GetValue(effectiveLeft);
                stateSave.SetValue(effectiveLeft, value, instanceLeft);
            }

            return new VariableReferenceAssignmentResult
            {
                NewValue = value,
                OldValue = valueBefore,
                VariableName = effectiveLeft
            };
        }

        private static object GetRightSideValue(StateSave stateSave, string right, string leftSideType)
        {
            if(CustomEvaluateExpression != null)
            {
                return CustomEvaluateExpression(stateSave, right, leftSideType);
            }
            else
            {
                // fallback (temporarily?)
                // assume the owner of the right-side is the StateSave that was passed in...
                var ownerOfRightSideVariable = stateSave;
                // ...but call this to change that in case the right-side is a variable belonging to some other component
                GetRightSideAndState(ref right, ref ownerOfRightSideVariable);

                var recursiveVariableFinder = new RecursiveVariableFinder(ownerOfRightSideVariable);

                var value = recursiveVariableFinder.GetValue(right);
                return value;
            }
        }

        public static void GetRightSideAndState(ref string right, ref StateSave stateSave)
        {
            var isExternalElement = right.Contains("/");

            if (isExternalElement)
            {
                var lastDot = right.LastIndexOf('.');
                var firstDot = right.IndexOf('.');

                var elementNameToFind = right.Substring(0, firstDot);

                if (elementNameToFind.StartsWith("Components/"))
                {
                    var stripped = elementNameToFind.Substring("Components/".Length);

                    var element = ObjectFinder.Self.GetComponent(stripped);

                    if (element != null)
                    {
                        stateSave = GetRightSide(ref right, firstDot, element);
                    }
                }
                else if (elementNameToFind.StartsWith("Screens/"))
                {
                    var stripped = elementNameToFind.Substring("Screens/".Length);

                    var element = ObjectFinder.Self.GetScreen(stripped);

                    if (element != null)
                    {
                        stateSave = GetRightSide(ref right, firstDot, element);
                    }
                }
            }
            else
            {
                
            }

        }

        private static StateSave GetRightSide(ref string right, int firstDot, ElementSave element)
        {
            StateSave stateSave = element.DefaultState;
            right = right.Substring(firstDot + 1);

            if (right.Contains("."))
            {
                var dotAfterInstance = right.IndexOf(".");
                var instanceName = right.Substring(0, dotAfterInstance);
                var instance = element.GetInstance(instanceName);

                if(instance != null)
                {
                    // we found the variable on the instance
                    // do nothing to right, it's got what we need there already
                }
                else
                {
                    GetRightSideAndState(ref right, ref stateSave);
                }
            }

            return stateSave;
        }

        public static void SetGraphicalUiElement(this ElementSave elementSave, GraphicalUiElement toReturn, ISystemManagers systemManagers)
        {
            // We need to set categories and states first since those are used below;
            toReturn.AddStatesAndCategoriesRecursivelyToGue(elementSave);

            if (toReturn.RenderableComponent == null)
            {
                // This could have already been created by the type that is instantiated, so don't do this to double-create
                toReturn.CreateGraphicalComponent(elementSave, systemManagers);
            }

            toReturn.AddExposedVariablesRecursively(elementSave);

            toReturn.CreateChildrenRecursively(elementSave, systemManagers);

            toReturn.Tag = elementSave;
            toReturn.ElementSave = elementSave;

            toReturn.SetInitialState();

            toReturn.AfterFullCreation();
        }

        public static void CreateChildrenRecursively(GraphicalUiElement graphicalUiElement, ElementSave elementSave, ISystemManagers systemManagers)
        {
            bool isScreen = elementSave is ScreenSave;

            foreach (var instance in elementSave.Instances)
            {
                var childGue = instance.ToGraphicalUiElement(systemManagers);

                if (childGue != null)
                {
                    if (!isScreen)
                    {
                        childGue.Parent = graphicalUiElement;
                    }
                    childGue.ElementGueContainingThis = graphicalUiElement;
                }
            }
        }
    }
}
