using CodeOutputPlugin.Models;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.Manager
{
    #region Enums

    public enum VisualApi
    {
        Gum,
        XamarinForms
    }

    #endregion

    public static class CodeGenerator
    {
        public static int CanvasWidth { get; set; } = 480;
        public static int CanvasHeight { get; set; } = 854;

        /// <summary>
        /// if true, then pixel sizes are maintained regardless of pixel density. This allows layouts to maintain pixel-perfect.
        /// Update: This is now set to false because .... well, it makes it hard to create flexible layouts. It's best to set a resolution of 
        /// 320 wide and let density scale things up
        /// </summary>
        public static bool AdjustPixelValuesForDensity { get; set; } = false;

        public static string GetGeneratedCodeForElement(ElementSave element, CodeOutputElementSettings elementSettings, CodeOutputProjectSettings projectSettings)
        {
            VisualApi visualApi = GetVisualApiForElement(element);

            var stringBuilder = new StringBuilder();
            int tabCount = 0;

            #region Using Statements

            if (!string.IsNullOrWhiteSpace(projectSettings?.CommonUsingStatements))
            {
                stringBuilder.AppendLine(projectSettings.CommonUsingStatements);
            }

            if (!string.IsNullOrEmpty(elementSettings?.UsingStatements))
            {
                stringBuilder.AppendLine(elementSettings.UsingStatements);
            }
            #endregion

            #region Namespace Header/Opening {

            string namespaceName = GetElementNamespace(element, elementSettings, projectSettings);

            if (!string.IsNullOrEmpty(namespaceName))
            {
                stringBuilder.AppendLine(ToTabs(tabCount) + $"namespace {namespaceName}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
            }

            #endregion

            #region Class Header/Opening {

            stringBuilder.AppendLine(ToTabs(tabCount) + $"partial class {GetClassNameForType(element.Name, visualApi)}");
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;
            #endregion

            FillWithStateEnums(element, stringBuilder, tabCount);

            FillWithCurrentState(element, stringBuilder, tabCount);

            foreach (var instance in element.Instances.Where(item => item.DefinedByBase == false))
            {
                FillWithInstanceDeclaration(instance, element, stringBuilder, tabCount);
            }

            AddAbsoluteLayoutIfNecessary(element, tabCount, stringBuilder);

            stringBuilder.AppendLine();

            FillWithExposedVariables(element, stringBuilder, visualApi, tabCount);
            // -- no need for AppendLine here since FillWithExposedVariables does it after every variable --

            GenerateConstructor(element, visualApi, tabCount, stringBuilder);

            stringBuilder.AppendLine(ToTabs(tabCount) + "partial void CustomInitialize();");

            #region Class Closing }
            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            #endregion

            if (!string.IsNullOrEmpty(namespaceName))
            {
                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }

            return stringBuilder.ToString();
        }

        public static string GetElementNamespace(ElementSave element, CodeOutputElementSettings elementSettings, CodeOutputProjectSettings projectSettings)
        {
            var namespaceName = elementSettings?.Namespace;

            if (string.IsNullOrEmpty(namespaceName) && !string.IsNullOrWhiteSpace(projectSettings.RootNamespace))
            {
                namespaceName = projectSettings.RootNamespace;
                if (element is ScreenSave)
                {
                    namespaceName += ".Screens";
                }
                else if (element is ComponentSave)
                {
                    namespaceName += ".Components";
                }
                else // standard element
                {
                    namespaceName += ".Standards";
                }

                var splitElementName = element.Name.Split('\\').ToArray();
                var splitPrefix = splitElementName.Take(splitElementName.Length - 1).ToArray();
                var whatToAppend = string.Join(".", splitPrefix);
                if (!string.IsNullOrEmpty(whatToAppend))
                {
                    namespaceName += "." + whatToAppend;
                }
            }

            return namespaceName;
        }

        public static VisualApi GetVisualApiForElement(ElementSave element)
        {
            VisualApi visualApi;
            var defaultState = element.DefaultState;
            var isXamForms = defaultState.GetValueRecursive($"IsXamarinFormsControl") as bool?;
            if (isXamForms == true)
            {
                visualApi = VisualApi.XamarinForms;
            }
            else
            {
                visualApi = VisualApi.Gum;
            }

            return visualApi;
        }

        private static void AddAbsoluteLayoutIfNecessary(ElementSave element, int tabCount, StringBuilder stringBuilder)
        {
            var elementBaseType = element?.BaseType;
            var isThisAbsoluteLayout = elementBaseType?.EndsWith("/AbsoluteLayout") == true;

            var isSkiaCanvasView = elementBaseType?.EndsWith("/SkiaGumCanvasView") == true;

            var isContainer = elementBaseType == "Container";

            if (!isThisAbsoluteLayout && !isSkiaCanvasView && !isContainer)
            {
                var shouldAddMainLayout = true;
                if (element is ScreenSave && !string.IsNullOrEmpty(element.BaseType))
                {
                    shouldAddMainLayout = false;
                }

                if (shouldAddMainLayout)
                {
                    stringBuilder.Append(ToTabs(tabCount) + "protected AbsoluteLayout MainLayout{get; private set;}");
                }
            }
        }

        private static void GenerateConstructor(ElementSave element, VisualApi visualApi, int tabCount, StringBuilder stringBuilder)
        {
            var elementName = GetClassNameForType(element.Name, visualApi);

            if(visualApi == VisualApi.Gum)
            {
                #region Constructor Header

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {elementName}(bool fullInstantiation = true)");

                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;

                #endregion

                #region Gum-required constructor code

                stringBuilder.AppendLine(ToTabs(tabCount) + "if(fullInstantiation)");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;

                if(element.BaseType == "Container")
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + "this.SetContainedObject(new InvisibleRenderable());");
                }

                stringBuilder.AppendLine();
                #endregion
            }
            else // xamarin forms
            {
                #region Constructor Header
                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {elementName}()");

                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;

                #endregion


                stringBuilder.AppendLine(ToTabs(tabCount) + "var wasSuspended = GraphicalUiElement.IsAllLayoutSuspended;");
                stringBuilder.AppendLine(ToTabs(tabCount) + "GraphicalUiElement.IsAllLayoutSuspended = true;");

                var elementBaseType = element?.BaseType;
                var isThisAbsoluteLayout = elementBaseType?.EndsWith("/AbsoluteLayout") == true;

                var isSkiaCanvasView = elementBaseType?.EndsWith("/SkiaGumCanvasView") == true;

                if(isThisAbsoluteLayout)
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + "var MainLayout = this;");
                }
                else if(!isSkiaCanvasView)
                {
                    var shouldAddMainLayout = true;
                    if(element is ScreenSave && !string.IsNullOrEmpty(element.BaseType))
                    {
                        shouldAddMainLayout = false;
                    }

                    if(shouldAddMainLayout)
                    {
                        stringBuilder.AppendLine(ToTabs(tabCount) + "MainLayout = new AbsoluteLayout();");
                        stringBuilder.AppendLine(ToTabs(tabCount) + "BaseGrid.Children.Add(MainLayout);");
                    }
                }

            }

            FillWithVariableAssignments(element, visualApi, stringBuilder, tabCount);

            stringBuilder.AppendLine();

            foreach (var instance in element.Instances.Where(item => item.DefinedByBase == false))
            {
                FillWithInstanceInstantiation(instance, element, stringBuilder, tabCount);
            }
            stringBuilder.AppendLine();

            // fill with variable binding after the instances have been created
            if(visualApi == VisualApi.XamarinForms)
            {
                FillWithVariableBinding(element, stringBuilder, tabCount);
            }

            foreach (var instance in element.Instances)
            {
                FillWithVariableAssignments(instance, element, stringBuilder, tabCount);
                stringBuilder.AppendLine();
            }

            stringBuilder.AppendLine(ToTabs(tabCount) + "CustomInitialize();");

            if(visualApi == VisualApi.Gum)
            {
                // close the if check
                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
            else
            {
                stringBuilder.AppendLine(ToTabs(tabCount) + "GraphicalUiElement.IsAllLayoutSuspended = wasSuspended;");

            }


            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
        }

        private static void FillWithVariableBinding(ElementSave element, StringBuilder stringBuilder, int tabCount)
        {
            var boundVariables = new List<VariableSave>();

            foreach (var variable in element.DefaultState.Variables)
            {
                if (!string.IsNullOrEmpty(variable.ExposedAsName))
                {
                    var instanceName = variable.SourceObject;
                    // make sure this instance is a XamForms object otherwise we don't need to set the binding
                    var isXamForms = (element.DefaultState.GetValueRecursive($"{instanceName}.IsXamarinFormsControl") as bool?) ?? false;

                    if(isXamForms)
                    {
                        var instance = element.GetInstance(instanceName);
                        var instanceType = GetClassNameForType(instance.BaseType, VisualApi.XamarinForms); 
                        stringBuilder.AppendLine(ToTabs(tabCount) + $"{instanceName}.SetBinding({instanceType}.{variable.GetRootName()}Property, nameof({variable.ExposedAsName}));");
                    }
                }
            
            }
        }

        public static string GetCodeForState(ElementSave container, StateSave stateSave, VisualApi visualApi)
        {
            var stringBuilder = new StringBuilder();

            FillWithVariablesInState(container, stateSave, stringBuilder, 0);

            var code = stringBuilder.ToString();
            return code;
        }

        private static void FillWithVariablesInState(ElementSave container, StateSave stateSave, StringBuilder stringBuilder, int tabCount)
        {
            VariableSave[] variablesToConsider = stateSave.Variables
                // make "Parent" first
                .Where(item => item.GetRootName() != "Parent")
                .ToArray();

            var variableGroups = variablesToConsider.GroupBy(item => item.SourceObject);


            foreach(var group in variableGroups)
            {
                InstanceSave instance = null;
                var instanceName = group.Key;

                if (instanceName != null)
                {
                    instance = container.GetInstance(instanceName);
                }

                #region Determine visual API (Gum or Forms)

                VisualApi visualApi = VisualApi.Gum;

                var defaultState = container.DefaultState;
                bool? isXamForms = false;
                if (instance == null)
                {
                    isXamForms = defaultState.GetValueRecursive($"IsXamarinFormsControl") as bool?;
                }
                else
                {
                    isXamForms = defaultState.GetValueRecursive($"{instance.Name}.IsXamarinFormsControl") as bool?;
                }
                if (isXamForms == true)
                {
                    visualApi = VisualApi.XamarinForms;
                }

                #endregion

                ElementSave baseElement = null;
                if(instance == null)
                {
                    baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(container.BaseType) ?? container;
                }
                else
                {
                    baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance?.BaseType);
                }

                // could be null if the element references an element that doesn't exist.
                if(baseElement != null)
                {
                    var baseDefaultState = baseElement?.DefaultState;
                    RecursiveVariableFinder baseRecursiveVariableFinder = new RecursiveVariableFinder(baseDefaultState);


                    List<VariableSave> variablesForThisInstance = group
                        .Where(item => GetIfVariableShouldBeIncludedForInstance(instance, item, baseRecursiveVariableFinder))
                        .ToList();


                    ProcessVariableGroups(variablesForThisInstance, stateSave, instance, container, visualApi, stringBuilder, tabCount);

                    // Now that they've been processed, we can process the remainder regularly
                    foreach (var variable in variablesForThisInstance)
                    {
                        var codeLine = GetCodeLine(instance, variable, container, visualApi, stateSave);
                        stringBuilder.AppendLine(ToTabs(tabCount) + codeLine);
                        var suffixCodeLine = GetSuffixCodeLine(instance, variable, visualApi);
                        if (!string.IsNullOrEmpty(suffixCodeLine))
                        {
                            stringBuilder.AppendLine(ToTabs(tabCount) + suffixCodeLine);
                        }
                    }
                }

            }
        }

        private static void FillWithStateEnums(ElementSave element, StringBuilder stringBuilder, int tabCount)
        {
            // for now we'll just do categories. We may need to get uncategorized at some point...
            foreach(var category in element.Categories)
            {
                string enumName = category.Name;

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public enum {category.Name}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;

                foreach(var state in category.States)
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"{state.Name},");
                }

                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
                tabCount--;
            }
        }

        private static void FillWithCurrentState(ElementSave element, StringBuilder stringBuilder, int tabCount)
        {
            foreach (var category in element.Categories)
            {
                stringBuilder.AppendLine();
                string enumName = category.Name;

                stringBuilder.AppendLine(ToTabs(tabCount) + $"{category.Name} m{category.Name}State;");
                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {category.Name} {category.Name}State");

                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"get => m{category.Name}State;");
                stringBuilder.AppendLine(ToTabs(tabCount) + $"set");

                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"m{category.Name}State = value;");

                stringBuilder.AppendLine(ToTabs(tabCount) + $"switch (value)");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;

                foreach(var state in category.States)
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"case {category.Name}.{state.Name}:");
                    tabCount++;

                    FillWithVariablesInState(element, state, stringBuilder, tabCount);

                    stringBuilder.AppendLine(ToTabs(tabCount) + $"break;");
                    tabCount--;
                }


                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");


                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");

                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
        }

        private static void FillWithExposedVariables(ElementSave element, StringBuilder stringBuilder, VisualApi visualApi, int tabCount)
        {
            var exposedVariables = element.DefaultState.Variables
                .Where(item => !string.IsNullOrEmpty(item.ExposedAsName))
                .ToArray();

            foreach(var exposedVariable in exposedVariables)
            {
                FillWithExposedVariable(exposedVariable, element, stringBuilder, tabCount);
                stringBuilder.AppendLine();
            }
        }

        private static void FillWithExposedVariable(VariableSave exposedVariable, ElementSave container, StringBuilder stringBuilder, int tabCount)
        {

            // if both the container and the instance are xamarin forms objects, then we can try to do some bubble-up binding
            var instanceName = exposedVariable.SourceObject;
            bool usesBinding = GetIfInstanceShouldBind(container, instanceName);
            var type = exposedVariable.Type;

            if (exposedVariable.IsState(container, out ElementSave stateContainer, out StateSaveCategory category))
            {
                var stateContainerType = GetClassNameForType(stateContainer.Name, VisualApi.Gum);
                type = $"{stateContainerType}.{category.Name}";
            }

            if (usesBinding)
            {
                var containerClassName = GetClassNameForType(container.Name, VisualApi.XamarinForms);
                stringBuilder.AppendLine($"{ToTabs(tabCount)}public static readonly BindableProperty {exposedVariable.ExposedAsName}Property = " +
                    $"BindableProperty.Create(nameof({exposedVariable.ExposedAsName}),typeof({type}),typeof({containerClassName}), defaultBindingMode: BindingMode.TwoWay);");

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public string {exposedVariable.ExposedAsName}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"get => ({type})GetValue({exposedVariable.ExposedAsName}Property);");
                stringBuilder.AppendLine(ToTabs(tabCount) + $"set => SetValue({exposedVariable.ExposedAsName}Property, value);");
                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
            else
            {

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {type} {exposedVariable.ExposedAsName}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"get => {exposedVariable.Name};");
                stringBuilder.AppendLine(ToTabs(tabCount) + $"set => {exposedVariable.Name} = value;");
                tabCount--;

                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
        }

        private static bool GetIfInstanceShouldBind(ElementSave container, string instanceName)
        {
            var isContainerXamarinForms = (container.DefaultState.GetValueRecursive("IsXamarinFormsControl") as bool?) ?? false;
            var isInstanceXamarinForms = (container.DefaultState.GetValueRecursive($"{instanceName}.IsXamarinFormsControl") as bool?) ?? false;
            var usesBinding = isContainerXamarinForms && isInstanceXamarinForms;
            return usesBinding;
        }

        public static string GetCodeForInstance(InstanceSave instance, ElementSave element, VisualApi visualApi)
        {
            var stringBuilder = new StringBuilder();

            FillWithInstanceDeclaration(instance, element, stringBuilder);

            FillWithInstanceInstantiation(instance, element, stringBuilder);

            FillWithVariableAssignments(instance, element, stringBuilder);

            var code = stringBuilder.ToString();
            return code;
        }

        private static void FillWithInstanceInstantiation(InstanceSave instance, ElementSave element, StringBuilder stringBuilder, int tabCount = 0)
        {
            var strippedType = instance.BaseType;
            if (strippedType.Contains("/"))
            {
                strippedType = strippedType.Substring(strippedType.LastIndexOf("/") + 1);
            }
            var tabs = new String(' ', 4 * tabCount);

            VisualApi visualApi = VisualApi.Gum;

            var defaultState = element.DefaultState;
            var isXamForms = (defaultState.GetValueRecursive($"{instance.Name}.IsXamarinFormsControl") as bool?) ?? false;
            if(isXamForms)
            {
                visualApi = VisualApi.XamarinForms;
            }

            stringBuilder.AppendLine($"{tabs}{instance.Name} = new {GetClassNameForType(instance.BaseType, visualApi)}();");

            var shouldSetBinding =
                isXamForms && defaultState.Variables.Any(item => !string.IsNullOrEmpty(item.ExposedAsName) && item.SourceObject == instance.Name);
            // If it's xamarin forms and we have exposed variables, then let's set up binding to this
            if (shouldSetBinding)
            {
                stringBuilder.AppendLine($"{tabs}{instance.Name}.BindingContext = this;");
            }
        }

        private static void FillWithVariableAssignments(ElementSave element, VisualApi visualApi, StringBuilder stringBuilder, int tabCount = 0)
        {
            #region Get variables to consider
            var defaultState = SelectedState.Self.SelectedElement.DefaultState;

            var baseElement = ObjectFinder.Self.GetElementSave(element.BaseType);
            RecursiveVariableFinder recursiveVariableFinder = null;

            // This is null if it's a screen, or there's some bad reference
            if(baseElement != null)
            {
                recursiveVariableFinder = new RecursiveVariableFinder(baseElement.DefaultState);
            }

            var variablesToConsider = defaultState.Variables
                .Where(item =>
                {
                    var shouldInclude = 
                        item.Value != null &&
                        item.SetsValue &&
                        string.IsNullOrEmpty(item.SourceObject);

                    if(shouldInclude)
                    {
                        if(recursiveVariableFinder != null)
                        {
                            // We want to make sure that the variable is defined in the base object. If it isn't, then
                            // it could be a leftover variable caused by having this object be of one type, using a variable
                            // specific to that type, then changing it to another type. Gum holds on to these varibles in case
                            // the type change was accidental, but it means we have to watch for these orphan variables when generating.
                            var foundVariable = recursiveVariableFinder.GetVariable(item.Name);
                            shouldInclude = foundVariable != null;
                        }
                        else
                        {
                            if(item.Name.EndsWith("State"))
                            {
                                var type = item.Type.Substring(item.Type.Length - 5);
                                var hasCategory = element.GetStateSaveCategoryRecursively(type) != null;

                                if(!hasCategory)
                                {
                                    shouldInclude = false;
                                }
                            }
                        }

                    }

                    return shouldInclude;
                })
                .ToList();

            #endregion

            var tabs = new String(' ', 4 * tabCount);

            ProcessVariableGroups(variablesToConsider, defaultState, null, element, visualApi, stringBuilder, tabCount);
            
            foreach (var variable in variablesToConsider)
            {
                var codeLine = GetCodeLine(null, variable, element, visualApi, defaultState);
                stringBuilder.AppendLine(tabs + codeLine);

                var suffixCodeLine = GetSuffixCodeLine(null, variable, visualApi);
                if (!string.IsNullOrEmpty(suffixCodeLine))
                {
                    stringBuilder.AppendLine(tabs + suffixCodeLine);
                }
            }
        }

        private static void FillWithVariableAssignments(InstanceSave instance, ElementSave container, StringBuilder stringBuilder, int tabCount = 0)
        {
            #region Get variables to consider

            var variablesToAssignValues = GetVariablesForValueAssignmentCode(instance)
                // make "Parent" first
                // .. actually we need to make parent last so that it can properly assign parent on scrollables
                .OrderBy(item => item.GetRootName() == "Parent")
                .ToList();

            #endregion

            #region Determine visual API (Gum or Forms)

            VisualApi visualApi = VisualApi.Gum;

            var defaultState = container.DefaultState;
            var isXamForms = defaultState.GetValueRecursive($"{instance.Name}.IsXamarinFormsControl") as bool?;
            if (isXamForms == true)
            {
                visualApi = VisualApi.XamarinForms;
            }

            #endregion

            var tabs = new String(' ', 4 * tabCount);

            #region Name/Automation Id

            if (visualApi == VisualApi.Gum)
            {
                stringBuilder.AppendLine($"{tabs}{instance.Name}.Name = \"{instance.Name}\";");
            }
            else
            {
                // If defined by base, then the automation ID will already be set there, and 
                // Xamarin.Forms doesn't like an automation ID being set 2x
                if(instance.DefinedByBase == false)
                {
                    stringBuilder.AppendLine($"{tabs}{instance.Name}.AutomationId = \"{instance.Name}\";");
                }
                if(instance.BaseType?.EndsWith("/StackLayout") == true)
                {
                    stringBuilder.AppendLine($"{tabs}{instance.Name}.Spacing = 0;");
                }
            }

            #endregion


            // sometimes variables have to be processed in groups. For example, RGB values
            // have to be assigned all at once in a Color value in XamForms;
            ProcessVariableGroups(variablesToAssignValues, container.DefaultState, instance, container, visualApi, stringBuilder, tabCount);

            foreach (var variable in variablesToAssignValues)
            {
                var codeLine = GetCodeLine(instance, variable, container, visualApi, defaultState);

                // the line of code could be " ", a string with a space. This happens
                // if we want to skip a variable so we dont return null or empty.
                // But we also don't want a ton of spaces generated.
                if(!string.IsNullOrWhiteSpace(codeLine))
                {
                    stringBuilder.AppendLine(tabs + codeLine);
                }

                var suffixCodeLine = GetSuffixCodeLine(instance, variable, visualApi);
                if (!string.IsNullOrEmpty(suffixCodeLine))
                {
                    stringBuilder.AppendLine(tabs + suffixCodeLine);
                }
            }

            // For scrollable GumContainers we need to have the parent assigned *after* the AbsoluteLayout rectangle:
            #region Assign Parent

            var hasParent = variablesToAssignValues.Any(item => item.GetRootName() == "Parent");

            if (!hasParent && !instance.DefinedByBase)
            {

                if(visualApi == VisualApi.Gum)
                {
                    // add it to "this"
                    stringBuilder.AppendLine($"{tabs}this.Children.Add({instance.Name});");
                }
                else // forms
                {
                    var instanceBaseType = instance.BaseType;

                    if(instanceBaseType.EndsWith("/GumCollectionView"))
                    {
                        stringBuilder.AppendLine($"{tabs}var tempFor{instance.Name} = GumScrollBar.CreateScrollableAbsoluteLayout({instance.Name}, ScrollableLayoutParentPlacement.Free);");
                        stringBuilder.AppendLine($"{tabs}MainLayout.Children.Add(tempFor{instance.Name});");
                    }
                    else if (instanceBaseType.EndsWith("/ScrollView"))
                    {
                        // assume that stack view will be at the base
                        //stringBuilder.AppendLine($"{tabs}MainLayout.Children.Add(tempFor{instance.Name});");
                        stringBuilder.AppendLine($"{tabs}this.Content = {instance.Name};");
                        
                    }
                    else
                    {
                        stringBuilder.AppendLine($"{tabs}MainLayout.Children.Add({instance.Name});");
                    }
                }
            }

            #endregion
        }

        private static void ProcessVariableGroups(List<VariableSave> variablesToConsider, StateSave defaultState, InstanceSave instance, ElementSave container, VisualApi visualApi, StringBuilder stringBuilder, int tabCount)
        {
            if(visualApi == VisualApi.XamarinForms)
            {
                string baseType = null;
                if (instance != null)
                {
                    baseType = instance.BaseType;
                }
                else
                {
                    baseType = container.BaseType;
                }
                switch(baseType)
                {
                    case "Text":
                        ProcessColorForLabel(variablesToConsider, defaultState, instance, stringBuilder);
                        ProcessPositionAndSize(variablesToConsider, defaultState, instance, container, stringBuilder, tabCount);
                        break;
                    default:
                        ProcessPositionAndSize(variablesToConsider, defaultState, instance, container, stringBuilder, tabCount);
                        break;
                }
            }
        }

        private static void ProcessColorForLabel(List<VariableSave> variablesToConsider, StateSave defaultState, InstanceSave instance, StringBuilder stringBuilder)
        {
            var instancePrefix = instance != null ? instance.Name + "." : string.Empty;
            var instanceName = instance.Name;
            var rfv = new RecursiveVariableFinder(defaultState);

            var red = rfv.GetValue<int>(instancePrefix + "Red");
            var green = rfv.GetValue<int>(instancePrefix + "Green");
            var blue = rfv.GetValue<int>(instancePrefix + "Blue");
            var alpha = rfv.GetValue<int>(instancePrefix + "Alpha");

            variablesToConsider.RemoveAll(item => item.Name == instancePrefix + "Red");
            variablesToConsider.RemoveAll(item => item.Name == instancePrefix + "Green");
            variablesToConsider.RemoveAll(item => item.Name == instancePrefix + "Blue");
            variablesToConsider.RemoveAll(item => item.Name == instancePrefix + "Alpha");

            stringBuilder.AppendLine($"{instanceName}.TextColor = Color.FromRgba({red}, {green}, {blue}, {alpha});");
        }

        private static void ProcessPositionAndSize(List<VariableSave> variablesToConsider, StateSave defaultState, InstanceSave instance, ElementSave container, StringBuilder stringBuilder, int tabCount)
        {
            //////////////////Early out/////////////////////
            if(container is ScreenSave && instance == null)
            {
                // screens can't be positioned
                return;
            }
            /////////////// End Early Out/////////////
            string prefix = instance?.Name == null ? "" : instance.Name + ".";

            var setsAny =
                defaultState.Variables.Any(item =>
                    item.Name == prefix + "X" ||
                    item.Name == prefix + "Y" ||
                    item.Name == prefix + "Width" ||
                    item.Name == prefix + "Height" ||

                    item.Name == prefix + "X Units" ||
                    item.Name == prefix + "Y Units" ||
                    item.Name == prefix + "Width Units" ||
                    item.Name == prefix + "Height Units"||
                    item.Name == prefix + "X Origin" ||
                    item.Name == prefix + "Y Origin" 
                    
                    );

            InstanceSave parent = null;
            if(instance != null)
            {
                var parentName = defaultState.GetValueRecursive( instance.Name + ".Parent") as string;
                if(!string.IsNullOrEmpty(parentName))
                {
                    parent = container.GetInstance(parentName);
                }
            }

            if(parent == null || parent.BaseType?.EndsWith("/AbsoluteLayout") == true)
            {
                // If this is part of an absolute layout, we put it in an absolute layout. This is the default

                SetAbsoluteLayoutPosition(variablesToConsider, defaultState, instance, container, stringBuilder, tabCount);
            }
            else //if(parent?.BaseType?.EndsWith("/StackLayout") == true)
            {
                SetNonAbsoluteLayoutPosition(variablesToConsider, defaultState, instance, stringBuilder, tabCount, prefix, parent.BaseType);
            }

        }

        private static void SetNonAbsoluteLayoutPosition(List<VariableSave> variablesToConsider, StateSave defaultState, InstanceSave instance, 
            StringBuilder stringBuilder, int tabCount, string prefix, string parentBaseType)
        {
            var variableFinder = new RecursiveVariableFinder(defaultState);

            var x = variableFinder.GetValue<float>(prefix + "X");
            var y = variableFinder.GetValue<float>(prefix + "Y");
            var width = variableFinder.GetValue<float>(prefix + "Width");
            var height = variableFinder.GetValue<float>(prefix + "Height");

            var xUnits = variableFinder.GetValue<PositionUnitType>(prefix + "X Units");
            var yUnits = variableFinder.GetValue<PositionUnitType>(prefix + "Y Units");
            var widthUnits = variableFinder.GetValue<DimensionUnitType>(prefix + "Width Units");
            var heightUnits = variableFinder.GetValue<DimensionUnitType>(prefix + "Height Units");

            var xOrigin = variableFinder.GetValue<HorizontalAlignment>(prefix + "X Origin");
            var yOrigin = variableFinder.GetValue<VerticalAlignment>(prefix + "Y Origin");

            variablesToConsider.RemoveAll(item => item.Name == prefix + "X");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "Y");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "Width");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "Height");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "X Units");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "Y Units");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "Width Units");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "Height Units");

            if (widthUnits == DimensionUnitType.Absolute)
            {
                stringBuilder.AppendLine(
                    $"{ToTabs(tabCount)}{instance?.Name ?? "this"}.WidthRequest = {width.ToString(CultureInfo.InvariantCulture)}f;");
            }

            if (heightUnits == DimensionUnitType.Absolute)
            {
                stringBuilder.AppendLine(
                    $"{ToTabs(tabCount)}{instance?.Name ?? "this"}.HeightRequest = {height.ToString(CultureInfo.InvariantCulture)}f;");
            }

            float leftMargin = 0;
            float rightMargin = 0;
            float topMargin = 0;
            float bottomMargin = 0;

            var isStackLayout = parentBaseType?.EndsWith("/StackLayout") == true;

            if (xUnits == PositionUnitType.PixelsFromLeft)
            {
                leftMargin = x;
            }
            if(xUnits == PositionUnitType.PixelsFromLeft && widthUnits == DimensionUnitType.RelativeToContainer)
            {
                rightMargin = -width - x;
            }

            if(yUnits == PositionUnitType.PixelsFromTop)
            {
                topMargin = y;
            }
            if(yUnits == PositionUnitType.PixelsFromTop && heightUnits == DimensionUnitType.RelativeToChildren)
            {
                if(isStackLayout == false)
                {
                    // If it's a stack layout, we don't want to subtract from here.
                    bottomMargin = -height - y;
                }
            }

            stringBuilder.AppendLine($"{ToTabs(tabCount)}{instance?.Name}.Margin = new Thickness(" +
                $"{leftMargin.ToString(CultureInfo.InvariantCulture)}, " +
                $"{topMargin.ToString(CultureInfo.InvariantCulture)}, " +
                $"{rightMargin.ToString(CultureInfo.InvariantCulture)}, " +
                $"{bottomMargin.ToString(CultureInfo.InvariantCulture)});");

            if (widthUnits == DimensionUnitType.Absolute || widthUnits == DimensionUnitType.RelativeToChildren)
            {
                stringBuilder.AppendLine(
                    $"{ToTabs(tabCount)}{instance?.Name ?? "this"}.HorizontalOptions = LayoutOptions.Start;");
            }
            else if(widthUnits == DimensionUnitType.RelativeToContainer)
            {
                stringBuilder.AppendLine(
                    $"{ToTabs(tabCount)}{instance?.Name ?? "this"}.HorizontalOptions = LayoutOptions.Fill;");
            }
        }

        private static void SetAbsoluteLayoutPosition(List<VariableSave> variablesToConsider, StateSave defaultState, InstanceSave instance, ElementSave container, StringBuilder stringBuilder, int tabCount)
        {
            string prefix = instance?.Name == null ? "" : instance.Name + ".";

            var variableFinder = new RecursiveVariableFinder(defaultState);

            #region Get recursive values for position and size

            var x = variableFinder.GetValue<float>(prefix + "X");
            var y = variableFinder.GetValue<float>(prefix + "Y");
            var width = variableFinder.GetValue<float>(prefix + "Width");
            var height = variableFinder.GetValue<float>(prefix + "Height");

            var xUnits = variableFinder.GetValue<PositionUnitType>(prefix + "X Units");
            var yUnits = variableFinder.GetValue<PositionUnitType>(prefix + "Y Units");
            var widthUnits = variableFinder.GetValue<DimensionUnitType>(prefix + "Width Units");
            var heightUnits = variableFinder.GetValue<DimensionUnitType>(prefix + "Height Units");

            var xOrigin = variableFinder.GetValue<HorizontalAlignment>(prefix + "X Origin");
            var yOrigin = variableFinder.GetValue<VerticalAlignment>(prefix + "Y Origin");

            variablesToConsider.RemoveAll(item => item.Name == prefix + "X");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "Y");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "Width");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "Height");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "X Units");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "Y Units");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "Width Units");
            variablesToConsider.RemoveAll(item => item.Name == prefix + "Height Units");

            #endregion

            var proportionalFlags = new HashSet<string>();

            const string WidthProportionalFlag = "AbsoluteLayoutFlags.WidthProportional";
            const string HeightProportionalFlag = "AbsoluteLayoutFlags.HeightProportional";
            const string XProportionalFlag = "AbsoluteLayoutFlags.XProportional";
            const string YProportionalFlag = "AbsoluteLayoutFlags.YProportional";

            int leftMargin = 0;
            int topMargin = 0;
            int rightMargin = 0;
            int bottomMargin = 0;

            if (widthUnits == DimensionUnitType.Percentage)
            {
                width /= 100.0f;
                proportionalFlags.Add(WidthProportionalFlag);
            }
            else if (widthUnits == DimensionUnitType.RelativeToContainer)
            {
                if (width == 0)
                {
                    width = 1;
                    proportionalFlags.Add(WidthProportionalFlag);
                }
                else
                {
                    width = CalculateAbsoluteWidth(instance, container, variableFinder);
                }
            }
            else if (widthUnits == DimensionUnitType.RelativeToChildren)
            {
                // in this case we want to auto-size, which is what -1 indicates
                width = -1;
            }
            if (heightUnits == DimensionUnitType.Percentage)
            {
                height /= 100.0f;
                proportionalFlags.Add(HeightProportionalFlag);
            }
            else if (heightUnits == DimensionUnitType.RelativeToContainer)
            {
                if (height == 0)
                {
                    height = 1;
                    proportionalFlags.Add(HeightProportionalFlag);
                }
                else
                {
                    height = CalculateAbsoluteHeight(instance, container, variableFinder);

                }
            }
            if (heightUnits == DimensionUnitType.RelativeToChildren)
            {
                // see above on width relative to container for information
                height = -1;
            }

            // special case
            // If we're using the center with x=0 we'll pretend it's the same as 50% 
            if (xUnits == PositionUnitType.PixelsFromCenterX &&
                // why does the width unit even matter? Should be the same regardless of width unit...
                //widthUnits == DimensionUnitType.Absolute && 
                xOrigin == HorizontalAlignment.Center)
            {
                if (x == 0)
                {
                    // treat it like it's 50%:
                    x = .5f;
                    proportionalFlags.Add(XProportionalFlag);
                }
            }
            // Xamarin forms uses a weird anchoring system to combine both position and anchor into one value. Gum splits those into two values
            // We need to convert from the gum units to xamforms units:
            // for now assume it's all %'s:

            else if (xUnits == PositionUnitType.PercentageWidth)
            {
                x /= 100.0f;
                var adjustedCanvasWidth = 1 - width;
                if (adjustedCanvasWidth > 0)
                {
                    x /= adjustedCanvasWidth;
                }
                proportionalFlags.Add(XProportionalFlag);
            }
            else if (xUnits == PositionUnitType.PixelsFromLeft)
            {

            }
            else if (xUnits == PositionUnitType.PixelsFromCenterX)
            {
                if (widthUnits == DimensionUnitType.Absolute)
                {
                    x = (CanvasWidth - width) / 2.0f;
                }
            }
            else if (xUnits == PositionUnitType.PixelsFromRight)
            {
                if (xOrigin == HorizontalAlignment.Right)
                {
                    rightMargin = MathFunctions.RoundToInt(-x);
                    x = 1;
                    proportionalFlags.Add(XProportionalFlag);
                }
            }

            if (yUnits == PositionUnitType.PixelsFromCenterY && heightUnits == DimensionUnitType.Absolute && yOrigin == VerticalAlignment.Center)
            {
                if (y == 0)
                {
                    y = .5f;
                    proportionalFlags.Add(YProportionalFlag);
                }
            }
            else if (yUnits == PositionUnitType.PercentageHeight)
            {
                y /= 100.0f;
                var adjustedCanvasHeight = 1 - height;
                if (adjustedCanvasHeight > 0)
                {
                    y /= adjustedCanvasHeight;
                }
                proportionalFlags.Add(YProportionalFlag);
            }
            else if (yUnits == PositionUnitType.PixelsFromCenterY)
            {
                if (heightUnits == DimensionUnitType.Absolute)
                {
                    y = (CanvasHeight - height) / 2.0f;
                }
            }
            else if (yUnits == PositionUnitType.PixelsFromBottom)
            {
                y += CanvasHeight;

                if (yOrigin == VerticalAlignment.Bottom)
                {
                    y -= height;
                }
            }




            var xString = x.ToString(CultureInfo.InvariantCulture) + "f";
            var yString = y.ToString(CultureInfo.InvariantCulture) + "f";
            var widthString = width.ToString(CultureInfo.InvariantCulture) + "f";
            var heightString = height.ToString(CultureInfo.InvariantCulture) + "f";

            if (AdjustPixelValuesForDensity)
            {
                if (proportionalFlags.Contains(XProportionalFlag) == false)
                {
                    xString += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
                }
                if (proportionalFlags.Contains(YProportionalFlag) == false)
                {
                    yString += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
                }
                if (proportionalFlags.Contains(WidthProportionalFlag) == false)
                {
                    widthString += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
                }
                if (proportionalFlags.Contains(HeightProportionalFlag) == false)
                {
                    heightString += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
                }
            }

            var instanceOrThis =
                instance?.Name ?? "this";
            string boundsText =
                $"{ToTabs(tabCount)}AbsoluteLayout.SetLayoutBounds({instanceOrThis}, new Rectangle({xString}, {yString}, {widthString}, {heightString}));";
            string flagsText = null;
            if (proportionalFlags.Count > 0)
            {
                string flagsArguments = null;
                int i = 0;
                foreach (var flag in proportionalFlags)
                {
                    if (i > 0)
                    {
                        flagsArguments += " | ";
                    }
                    flagsArguments += flag;
                    i++;
                }
                flagsText = $"{ToTabs(tabCount)}AbsoluteLayout.SetLayoutFlags({instanceOrThis}, {flagsArguments});";
            }
            // assume every object has X, which it won't, so we will have to improve this
            if (string.IsNullOrWhiteSpace(flagsText))
            {
                stringBuilder.AppendLine(boundsText);
            }
            else
            {
                stringBuilder.AppendLine($"{boundsText}\n{flagsText}");
            }

            // not sure why these apply even though we're using values on the AbsoluteLayout
            if (!proportionalFlags.Contains(WidthProportionalFlag) && (widthUnits == DimensionUnitType.RelativeToContainer || widthUnits == DimensionUnitType.Absolute))
            {
                stringBuilder.AppendLine($"{ToTabs(tabCount)}{instanceOrThis}.WidthRequest = {width.ToString(CultureInfo.InvariantCulture)}f;");
            }
            if (!proportionalFlags.Contains(HeightProportionalFlag) && heightUnits == DimensionUnitType.RelativeToContainer || heightUnits == DimensionUnitType.Absolute)
            {
                stringBuilder.AppendLine($"{ToTabs(tabCount)}{instanceOrThis}.HeightRequest = {height.ToString(CultureInfo.InvariantCulture)}f;");
            }

            //If the object is width proportional, then it must use a .HorizontalOptions = LayoutOptions.Fill; or else the proportional width won't apply
            if (proportionalFlags.Contains(WidthProportionalFlag))
            {
                stringBuilder.AppendLine($"{ToTabs(tabCount)}{instanceOrThis}.HorizontalOptions = LayoutOptions.Fill;");
            }

            if (leftMargin != 0 || rightMargin != 0 || topMargin != 0 || bottomMargin != 0)
            {
                stringBuilder.AppendLine($"{ToTabs(tabCount)}{instanceOrThis}.Margin = new Thickness({leftMargin}, {topMargin}, {rightMargin}, {bottomMargin});");
            }
            // should we do the same to vertical? Maybe, but waiting for a natural use case to test it
        }

        private static float CalculateAbsoluteWidth(InstanceSave instance, ElementSave container, RecursiveVariableFinder variableFinder)
        {
            string prefix = instance?.Name == null ? "" : instance.Name + ".";


            var x = variableFinder.GetValue<float>(prefix + "X");
            var width = variableFinder.GetValue<float>(prefix + "Width");

            var xUnits = variableFinder.GetValue<PositionUnitType>(prefix + "X Units");
            var widthUnits = variableFinder.GetValue<DimensionUnitType>(prefix + "Width Units");

            var xOrigin = variableFinder.GetValue<HorizontalAlignment>(prefix + "X Origin");

            var parentName = variableFinder.GetValue<string>(prefix + "Parent");

            var parent = container.GetInstance(parentName);

            float toReturn = 0;
            if (instance == null)
            {
                toReturn = width; // handle this eventually?
            }
            else if(widthUnits == DimensionUnitType.Absolute)
            {
                toReturn = width;
            }
            else if(widthUnits == DimensionUnitType.RelativeToContainer)
            {
                if(parent == null)
                {
                    toReturn = width + GumState.Self.ProjectState.GumProjectSave.DefaultCanvasWidth;
                }
                else
                {
                    var parentWidth = CalculateAbsoluteWidth(parent, container, variableFinder);

                    toReturn = parentWidth + width;
                }
            }

            return toReturn;
        }

        private static float CalculateAbsoluteHeight(InstanceSave instance, ElementSave container, RecursiveVariableFinder variableFinder)
        {
            string prefix = instance?.Name == null ? "" : instance.Name + ".";


            var y = variableFinder.GetValue<float>(prefix + "Y");
            var height = variableFinder.GetValue<float>(prefix + "Height");

            var yUnits = variableFinder.GetValue<PositionUnitType>(prefix + "Y Units");
            var heightUnits = variableFinder.GetValue<DimensionUnitType>(prefix + "Height Units");

            var yOrigin = variableFinder.GetValue<HorizontalAlignment>(prefix + "Y Origin");

            var parentName = variableFinder.GetValue<string>(prefix + "Parent");

            var parent = container.GetInstance(parentName);

            float toReturn = 0;
            if (instance == null)
            {
                toReturn = height; // handle this eventually?
            }
            else if (heightUnits == DimensionUnitType.Absolute)
            {
                toReturn = height;
            }
            else if (heightUnits == DimensionUnitType.RelativeToContainer)
            {
                if (parent == null)
                {
                    toReturn = height + GumState.Self.ProjectState.GumProjectSave.DefaultCanvasHeight;
                }
                else
                {
                    var parentWidth = CalculateAbsoluteHeight(parent, container, variableFinder);

                    toReturn = parentWidth + height;
                }
            }

            return toReturn;
        }

        private static void FillWithInstanceDeclaration(InstanceSave instance, ElementSave container, StringBuilder stringBuilder, int tabCount = 0)
        {
            VisualApi visualApi = VisualApi.Gum;

            var defaultState = container.DefaultState;
            var isXamForms = defaultState.GetValueRecursive($"{instance.Name}.IsXamarinFormsControl") as bool?;
            if (isXamForms == true)
            {
                visualApi = VisualApi.XamarinForms;
            }

            var tabs = new String(' ', 4 * tabCount);

            string className = GetClassNameForType(instance.BaseType, visualApi);

            bool isPublic = true;
            string accessString = isPublic ? "public " : "";

            stringBuilder.AppendLine($"{tabs}{accessString}{className} {instance.Name} {{ get; private set; }}");
        }

        public static string GetClassNameForType(string gumType, VisualApi visualApi)
        {
            string className = null;
            var specialHandledCase = false;

            if(visualApi == VisualApi.XamarinForms)
            {
                switch(gumType)
                {
                    case "Text":
                        className = "Label";
                        specialHandledCase = true;
                        break;
                }
            }

            if(!specialHandledCase)
            {

                var strippedType = gumType;
                if (strippedType.Contains("/"))
                {
                    strippedType = strippedType.Substring(strippedType.LastIndexOf("/") + 1);
                }

                string suffix = visualApi == VisualApi.Gum ? "Runtime" : "";
                className = $"{strippedType}{suffix}";

            }
            return className;
        }

        private static string GetSuffixCodeLine(InstanceSave instance, VariableSave variable, VisualApi visualApi)
        {
            if(visualApi == VisualApi.XamarinForms)
            {
                var rootName = variable.GetRootName();

                //switch(rootName)
                //{
                    // We don't do this anymore now that we are stuffing forms objects in absolute layouts
                    //case "Width": return $"{instance.Name}.HorizontalOptions = LayoutOptions.Start;";
                    //case "Height": return $"{instance.Name}.VerticalOptions = LayoutOptions.Start;";
                //}
            }

            return null;
        }

        private static string GetCodeLine(InstanceSave instance, VariableSave variable, ElementSave container, VisualApi visualApi, StateSave state)
        {
            string instancePrefix = instance != null ? $"{instance.Name}." : "this.";

            if (visualApi == VisualApi.Gum)
            {
                var fullLineReplacement = TryGetFullGumLineReplacement(instance, variable);

                if(fullLineReplacement != null)
                {
                    return fullLineReplacement;
                }
                else
                {
                    return $"{instancePrefix}{GetGumVariableName(variable, container)} = {VariableValueToGumCodeValue(variable, container)};";
                }

            }
            else // xamarin forms
            {
                var fullLineReplacement = TryGetFullXamarinFormsLineReplacement(instance, container, variable, state);
                if(fullLineReplacement != null)
                {
                    return fullLineReplacement;
                }
                else
                {
                    return $"{instancePrefix}{GetXamarinFormsVariableName(variable)} = {VariableValueToXamarinFormsCodeValue(variable, container)};";
                }

            }
        }

        private static string TryGetFullXamarinFormsLineReplacement(InstanceSave instance, ElementSave container, VariableSave variable, StateSave state)
        {
            var rootName = variable.GetRootName();
            
            if(rootName == "IsXamarinFormsControl" ||
                rootName == "Name" ||
                rootName == "X Origin" ||
                rootName == "XOrigin" ||
                rootName == "Y Origin" ||
                rootName == "YOrigin")
            {
                return " "; // Don't do anything with these variables::
            }
            else if(rootName == "Parent")
            {
                var parentName = variable.Value as string;

                var parentInstance = container.GetInstance(parentName);

                var hasContent =
                    parentInstance?.BaseType.EndsWith("/ScrollView") == true ||
                    parentInstance?.BaseType.EndsWith("/StickyScrollView") == true;
                if(hasContent)
                {
                    return $"{parentName}.Content = {instance.Name};";
                }
                else
                {
                    return $"{parentName}.Children.Add({instance.Name});";
                }
            }

            #region Children Layout

            else if (rootName == "Children Layout")
            {
                if (instance.BaseType.EndsWith("/StackLayout") && variable.Value is ChildrenLayout valueAsChildrenLayout)
                {
                    if(valueAsChildrenLayout == ChildrenLayout.LeftToRightStack)
                    {
                        return $"{instance.Name}.Orientation = StackOrientation.Horizontal;";
                    }
                    else
                    {
                        return $"{instance.Name}.Orientation = StackOrientation.Vertical;";
                    }
                }
            }

            #endregion

            return null;
        }

        private static string TryGetFullGumLineReplacement(InstanceSave instance, VariableSave variable)
        {
            var rootName = variable.GetRootName();
            #region Parent

            if (rootName == "Parent")
            {
                return $"{variable.Value}.Children.Add({instance.Name});";
            }
            #endregion



                    // ignored variables:
            else if (rootName == "IsXamarinFormsControl" ||
                rootName == "ClipsChildren" ||
                rootName == "ExposeChildrenEvents" ||
                rootName == "HasEvents")
            {
                return " "; 
            }
            return null;
        }

        private static string VariableValueToGumCodeValue(VariableSave variable, ElementSave container)
        {
            if(variable.Value is float asFloat)
            {
                return asFloat.ToString(CultureInfo.InvariantCulture) + "f";
            }
            else if(variable.Value is string asString)
            {
                if(variable.GetRootName() == "Parent")
                {
                    return asString;
                }
                else if(variable.IsState(container, out ElementSave categoryContainer, out StateSaveCategory category))
                {
                    if(categoryContainer != null && category != null)
                    {
                        string containerClassName = "VariableState";
                        if (categoryContainer != null)
                        {
                            containerClassName = GetClassNameForType(categoryContainer.Name, VisualApi.Gum);
                        }
                        return $"{containerClassName}.{category.Name}.{asString}";
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return "\"" + asString.Replace("\n", "\\n") + "\"";
                }
            }
            else if(variable.Value is bool)
            {
                return variable.Value.ToString().ToLowerInvariant();
            }
            else if(variable.Value.GetType().IsEnum)
            {
                var type = variable.Value.GetType();
                if(type == typeof(PositionUnitType))
                {
                    var converted = UnitConverter.ConvertToGeneralUnit(variable.Value);
                    return $"GeneralUnitType.{converted}";
                }
                else
                {
                    return variable.Value.GetType().Name + "." + variable.Value.ToString();
                }
            }
            else
            {
                return variable.Value?.ToString();
            }
        }

        private static string VariableValueToXamarinFormsCodeValue(VariableSave variable, ElementSave container)
        {
            if (variable.Value is float asFloat)
            {
                var rootName = variable.GetRootName();
                // X and Y go to PixelX and PixelY
                if(rootName == "X" || rootName == "Y")
                {
                    return asFloat.ToString(CultureInfo.InvariantCulture) + "f";
                }
                else if(rootName == "CornerRadius")
                {
                    return $"(int)({asFloat.ToString(CultureInfo.InvariantCulture)} / DeviceDisplay.MainDisplayInfo.Density)";
                }
                else
                {
                    return $"{asFloat.ToString(CultureInfo.InvariantCulture)} / DeviceDisplay.MainDisplayInfo.Density";
                }
            }
            else if (variable.Value is string asString)
            {
                if (variable.GetRootName() == "Parent")
                {
                    return variable.Value.ToString();
                }
                else if (variable.IsState(container, out ElementSave categoryContainer, out StateSaveCategory category))
                {
                    var containerClassName = GetClassNameForType(categoryContainer.Name, VisualApi.XamarinForms);
                    return $"{containerClassName}.{category.Name}.{variable.Value}";
                }
                else
                {
                    return "\"" + asString.Replace("\n", "\\n") + "\"";
                }
            }
            else if(variable.Value is bool)
            {
                return variable.Value.ToString().ToLowerInvariant();
            }
            else if (variable.Value.GetType().IsEnum)
            {
                var type = variable.Value.GetType();
                if (type == typeof(PositionUnitType))
                {
                    var converted = UnitConverter.ConvertToGeneralUnit(variable.Value);
                    return $"GeneralUnitType.{converted}";
                }
                else if(type == typeof(HorizontalAlignment))
                {
                    switch((HorizontalAlignment)variable.Value)
                    {
                        case HorizontalAlignment.Left:
                            return "Xamarin.Forms.TextAlignment.Start";
                        case HorizontalAlignment.Center:
                            return "Xamarin.Forms.TextAlignment.Center";
                        case HorizontalAlignment.Right:
                            return "Xamarin.Forms.TextAlignment.End";
                        default:
                            return "";
                    }
                }
                else if(type == typeof(VerticalAlignment))
                {
                    switch((VerticalAlignment)variable.Value)
                    {
                        case VerticalAlignment.Top:
                            return "Xamarin.Forms.TextAlignment.Start";
                        case VerticalAlignment.Center:
                            return "Xamarin.Forms.TextAlignment.Center";
                        case VerticalAlignment.Bottom:
                            return "Xamarin.Forms.TextAlignment.End";
                        default:
                            return "";
                    }
                }
                else
                {
                    return variable.Value.GetType().Name + "." + variable.Value.ToString();
                }
            }
            else
            {
                return variable.Value?.ToString();
            }
        }

        private static object GetGumVariableName(VariableSave variable, ElementSave container)
        {
            if(variable.IsState(container))
            {
                return variable.GetRootName().Replace(" ", "");
            }
            else
            {
                return variable.GetRootName().Replace(" ", "");
            }
        }

        private static string GetXamarinFormsVariableName(VariableSave variable)
        {
            var rootName = variable.GetRootName();

            switch(rootName)
            {
                case "Height": return "HeightRequest";
                case "Width": return "WidthRequest";
                case "X": return "PixelX";
                case "Y": return "PixelY";
                case "Visible": return "IsVisible";
                case "HorizontalAlignment": return "HorizontalTextAlignment";
                case "VerticalAlignment": return "VerticalTextAlignment";

                default: return rootName;
            }
        }

        private static VariableSave[] GetVariablesForValueAssignmentCode(InstanceSave instance)
        {
            var baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance.BaseType);
            if(baseElement == null)
            {
                // this could happen if the project references an object that has a missing type. Tolerate it, return an empty l ist
                return new VariableSave[0];
            }
            else
            {
                var baseDefaultState = baseElement?.DefaultState;
                RecursiveVariableFinder baseRecursiveVariableFinder = new RecursiveVariableFinder(baseDefaultState);

                var defaultState = SelectedState.Self.SelectedElement.DefaultState;
                var variablesToConsider = defaultState.Variables
                    .Where(item =>
                    {
                        return GetIfVariableShouldBeIncludedForInstance(instance, item, baseRecursiveVariableFinder);
                    })
                    .ToArray();
                return variablesToConsider;
            }
        }

        private static bool GetIfVariableShouldBeIncludedForInstance(InstanceSave instance, VariableSave item, RecursiveVariableFinder baseRecursiveVariableFinder)
        {
            var shouldInclude =
                                    item.Value != null &&
                                    item.SetsValue &&
                                    item.SourceObject == instance?.Name;

            if (shouldInclude)
            {
                var foundVariable = baseRecursiveVariableFinder.GetVariable(item.GetRootName());
                shouldInclude = foundVariable != null;
            }

            return shouldInclude;
        }

        private static string ToTabs(int tabCount) => new string(' ', tabCount * 4);
    }
}
