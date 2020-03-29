using CodeOutputPlugin.Models;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.Manager
{
    public enum VisualApi
    {
        Gum,
        XamarinForms
    }

    public static class CodeGenerator
    {

        public static string GetCodeForElement(ElementSave element, VisualApi visualApi, CodeOutputElementSettings settings)
        {
            #region Determine if XamarinForms Control
            var defaultState = element.DefaultState;
            var isXamForms = defaultState.GetValueRecursive($"IsXamarinFormsControl") as bool?;
            if (isXamForms == true)
            {
                visualApi = VisualApi.XamarinForms;
            }
            #endregion

            var stringBuilder = new StringBuilder();
            int tabCount = 0;

            #region Using Statements

            if (!string.IsNullOrEmpty(settings?.UsingStatements))
            {
                stringBuilder.AppendLine(settings.UsingStatements);
            }
            #endregion

            #region Namespace Header/Opening {

            if (!string.IsNullOrEmpty(settings?.Namespace))
            {
                stringBuilder.AppendLine(ToTabs(tabCount) + $"namespace {settings.Namespace}");
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

            foreach (var instance in element.Instances)
            {
                FillWithInstanceDeclaration(instance, element, stringBuilder, tabCount);
            }
            stringBuilder.AppendLine();

            FillWithExposedVariables(element, stringBuilder, visualApi, tabCount);
            // -- no need for AppendLine here since FillWithExposedVariables does it after every variable --

            GenerateConstructor(element, visualApi, tabCount, stringBuilder);

            stringBuilder.AppendLine(ToTabs(tabCount) + "partial void CustomInitialize();");

            #region Class Closing }
            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            #endregion

            if (!string.IsNullOrEmpty(settings?.Namespace))
            {
                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }

            return stringBuilder.ToString();
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

                stringBuilder.AppendLine(ToTabs(tabCount) + "this.SetContainedObject(new InvisibleRenderable());");

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

                stringBuilder.AppendLine(ToTabs(tabCount) + "GraphicalUiElement.IsAllLayoutSuspended = true;");

                var isThisAbsoluteLayout = element?.BaseType?.EndsWith("/AbsoluteLayout") == true;

                if(isThisAbsoluteLayout)
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + "var layout = this;");
                }
                else
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + "var layout = new AbsoluteLayout();");
                    stringBuilder.AppendLine(ToTabs(tabCount) + "MainGrid.Children.Add(layout);");
                }

            }

            FillWithVariableAssignments(element, visualApi, stringBuilder, tabCount);

            stringBuilder.AppendLine();

            foreach (var instance in element.Instances)
            {
                FillWithInstanceInstantiation(instance, element, stringBuilder, tabCount);
            }
            stringBuilder.AppendLine();

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
                stringBuilder.AppendLine(ToTabs(tabCount) + "GraphicalUiElement.IsAllLayoutSuspended = false;");

            }


            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
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


                List<VariableSave> variablesForThisInstance = group.ToList();
                ProcessVariableGroups(variablesForThisInstance, stateSave, instance, visualApi, stringBuilder);

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
            var type = exposedVariable.Type;

            if(exposedVariable.IsState(container, out ElementSave stateContainer, out StateSaveCategory category))
            {
                var stateContainerType = GetClassNameForType(stateContainer.Name, VisualApi.Gum);
                type = $"{stateContainerType}.{category.Name}";
            }

            stringBuilder.AppendLine(ToTabs(tabCount) + $"public {type} {exposedVariable.ExposedAsName}");
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;
            stringBuilder.AppendLine(ToTabs(tabCount) + $"get => {exposedVariable.Name};");
            stringBuilder.AppendLine(ToTabs(tabCount) + $"set => {exposedVariable.Name} = value;");
            tabCount--;

            stringBuilder.AppendLine(ToTabs(tabCount) + "}");

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
            var isXamForms = defaultState.GetValueRecursive($"{instance.Name}.IsXamarinFormsControl") as bool?;
            if(isXamForms == true)
            {
                visualApi = VisualApi.XamarinForms;
            }

            stringBuilder.AppendLine($"{tabs}{instance.Name} = new {GetClassNameForType(instance.BaseType, visualApi)}();");
        }

        private static void FillWithVariableAssignments(ElementSave element, VisualApi visualApi, StringBuilder stringBuilder, int tabCount = 0)
        {
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

                    if(shouldInclude && recursiveVariableFinder != null)
                    {
                        var foundVariable = recursiveVariableFinder.GetValue(item.Name);
                        shouldInclude = foundVariable != null;
                    }

                    return shouldInclude;
                })
                .ToArray();

            var tabs = new String(' ', 4 * tabCount);
            
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

            var variablesToConsider = GetVariablesToConsider(instance)
                // make "Parent" first
                .OrderBy(item => item.GetRootName() != "Parent")
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

            #region (Optionally) Assign Name

            if (visualApi == VisualApi.Gum)
            {
                stringBuilder.AppendLine($"{tabs}{instance.Name}.Name = \"{instance.Name}\";");
            }

            #endregion

            #region Assign Parent

            var hasParent = variablesToConsider.FirstOrDefault()?.GetRootName() == "Parent";
            if (!hasParent)
            {
                if(visualApi == VisualApi.Gum)
                {
                    // add it to "this"
                    stringBuilder.AppendLine($"{tabs}this.Children.Add({instance.Name});");
                }
                else // forms
                {
                    stringBuilder.AppendLine($"{tabs}layout.Children.Add({instance.Name});");
                }
            }

            #endregion


            // sometimes variables have to be processed in groups. For example, RGB values
            // have to be assigned all at once in a Color value in XamForms;
            ProcessVariableGroups(variablesToConsider, container.DefaultState, instance, visualApi, stringBuilder);

            foreach (var variable in variablesToConsider)
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
        }

        private static void ProcessVariableGroups(List<VariableSave> variablesToConsider, StateSave defaultState, InstanceSave instance, VisualApi visualApi, StringBuilder stringBuilder)
        {
            if(visualApi == VisualApi.XamarinForms)
            {
                switch(instance.BaseType)
                {
                    case "Text":
                        ProcessColorForLabel(variablesToConsider, defaultState, instance, stringBuilder);
                        ProcessPositionAndSize(variablesToConsider, defaultState, instance, stringBuilder);
                        break;
                    default:
                        ProcessPositionAndSize(variablesToConsider, defaultState, instance, stringBuilder);
                        break;
                }
            }
        }

        private static void ProcessColorForLabel(List<VariableSave> variablesToConsider, StateSave defaultState, InstanceSave instance, StringBuilder stringBuilder)
        {
            var instanceName = instance.Name;
            var rfv = new RecursiveVariableFinder(defaultState);

            var red = rfv.GetValue<int>(instanceName + ".Red");
            var green = rfv.GetValue<int>(instanceName + ".Green");
            var blue = rfv.GetValue<int>(instanceName + ".Blue");
            var alpha = rfv.GetValue<int>(instanceName + ".Alpha");

            variablesToConsider.RemoveAll(item => item.Name == instanceName + ".Red");
            variablesToConsider.RemoveAll(item => item.Name == instanceName + ".Green");
            variablesToConsider.RemoveAll(item => item.Name == instanceName + ".Blue");
            variablesToConsider.RemoveAll(item => item.Name == instanceName + ".Alpha");

            stringBuilder.AppendLine($"{instanceName}.TextColor = Color.FromRgba({red}, {green}, {blue}, {alpha});");
        }

        private static void ProcessPositionAndSize(List<VariableSave> variablesToConsider, StateSave defaultState, InstanceSave instance, StringBuilder stringBuilder)
        {
            var instanceName = instance?.Name;

            string prefix = instance == null ? "" : instanceName + ".";

            var setsAny =
                defaultState.Variables.Any(item =>
                    item.Name == prefix + "X" ||
                    item.Name == prefix + "Y" ||
                    item.Name == prefix + "Width" ||
                    item.Name == prefix + "Height" ||

                    item.Name == prefix + "X Units" ||
                    item.Name == prefix + "Y Units" ||
                    item.Name == prefix + "Width Units" ||
                    item.Name == prefix + "Height Units" );

            if(setsAny)
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

                variablesToConsider.RemoveAll(item => item.Name == instanceName + ".X");
                variablesToConsider.RemoveAll(item => item.Name == instanceName + ".Y");
                variablesToConsider.RemoveAll(item => item.Name == instanceName + ".Width");
                variablesToConsider.RemoveAll(item => item.Name == instanceName + ".Height");
                variablesToConsider.RemoveAll(item => item.Name == instanceName + ".X Units");
                variablesToConsider.RemoveAll(item => item.Name == instanceName + ".Y Units");
                variablesToConsider.RemoveAll(item => item.Name == instanceName + ".Width Units");
                variablesToConsider.RemoveAll(item => item.Name == instanceName + ".Height Units");

                List<string> proportionalFlags = new List<string>();


                if (widthUnits == DimensionUnitType.Percentage)
                {
                    width /= 100.0f;
                    proportionalFlags.Add("AbsoluteLayoutFlags.WidthProportional");
                }
                else if(widthUnits == DimensionUnitType.RelativeToContainer)
                {
                    if(width == 0)
                    {
                        width = 1;
                        proportionalFlags.Add("AbsoluteLayoutFlags.WidthProportional");
                    }
                    else
                    {
                        // not allowed!!!
                    }
                }
                if (heightUnits == DimensionUnitType.Percentage)
                {
                    height /= 100.0f;
                    proportionalFlags.Add("AbsoluteLayoutFlags.HeightProportional");
                }
                else if(heightUnits == DimensionUnitType.RelativeToContainer)
                {
                    if(height == 0)
                    {
                        height = 1;
                        proportionalFlags.Add("AbsoluteLayoutFlags.HeightProportional");
                    }
                    else
                    {
                        // not allowed!
                    }
                }

                // Xamarin forms uses a weird anchoring system to combine both position and anchor into one value. Gum splits those into two values
                // We need to convert from the gum units to xamforms units:
                // for now assume it's all %'s:
                if (xUnits == PositionUnitType.PercentageWidth)
                {
                    x /= 100.0f;
                    var adjustedCanvasWidth = 1 - width;
                    if (adjustedCanvasWidth > 0)
                    {
                        x /= adjustedCanvasWidth;
                    }
                    proportionalFlags.Add("AbsoluteLayoutFlags.XProportional");
                }
                if (yUnits == PositionUnitType.PercentageHeight)
                {
                    y /= 100.0f;
                    var adjustedCanvasHeight = 1 - height;
                    if (adjustedCanvasHeight > 0)
                    {
                        y /= adjustedCanvasHeight;
                    }
                    proportionalFlags.Add("AbsoluteLayoutFlags.YProportional");
                }


                var xString = x.ToString(CultureInfo.InvariantCulture) + "f";
                var yString = y.ToString(CultureInfo.InvariantCulture) + "f";
                var widthString = width.ToString(CultureInfo.InvariantCulture) + "f";
                var heightString = height.ToString(CultureInfo.InvariantCulture) + "f";



                string boundsText =
                    $"AbsoluteLayout.SetLayoutBounds({instance?.Name ?? "this"}, new Rectangle({xString}, {yString}, {widthString}, {heightString}));";
                string flagsText = null;
                if (proportionalFlags.Count > 0)
                {
                    string flagsArguments = null;
                    for (int i = 0; i < proportionalFlags.Count; i++)
                    {
                        if (i > 0)
                        {
                            flagsArguments += " | ";
                        }
                        flagsArguments += proportionalFlags[i];
                    }
                    flagsText = $"AbsoluteLayout.SetLayoutFlags({instance?.Name ?? "this"}, {flagsArguments});";
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
            }

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

            stringBuilder.AppendLine($"{tabs}{accessString}{className} {instance.Name};");
        }

        private static string GetClassNameForType(string gumType, VisualApi visualApi)
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

            return null;
        }

        private static string TryGetFullGumLineReplacement(InstanceSave instance, VariableSave variable)
        {
            var rootName = variable.GetRootName();
            if (rootName == "Parent")
            {
                return $"{variable.Value}.Children.Add({instance.Name});";
            }
            // ignored variables:
            else if(rootName == "IsXamarinFormsControl" ||
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
                    var containerClassName = GetClassNameForType(categoryContainer.Name, VisualApi.Gum);
                    return $"{containerClassName}.{category.Name}.{asString}";
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
                else
                {
                    return asFloat.ToString(CultureInfo.InvariantCulture) + " / DeviceDisplay.MainDisplayInfo.Density";
                }
            }
            else if (variable.Value is string)
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
                    return "\"" + variable.Value + "\"";
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

        private static VariableSave[] GetVariablesToConsider(InstanceSave instance)
        {
            var baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance.BaseType);
            var baseDefaultState = baseElement?.DefaultState;
            RecursiveVariableFinder baseRecursiveVariableFinder = new RecursiveVariableFinder(baseDefaultState);

            var defaultState = SelectedState.Self.SelectedElement.DefaultState;
            var variablesToConsider = defaultState.Variables
                .Where(item =>
                {
                    var shouldInclude = 
                        item.Value != null &&
                        item.SetsValue &&
                        item.SourceObject == instance.Name;

                    if(shouldInclude)
                    {
                        var foundVariable = baseRecursiveVariableFinder.GetVariable(item.GetRootName());
                        shouldInclude = foundVariable != null;
                    }

                    return shouldInclude;
                })
                .ToArray();
            return variablesToConsider;
        }

        private static string ToTabs(int tabCount) => new string(' ', tabCount * 4);
    }
}
