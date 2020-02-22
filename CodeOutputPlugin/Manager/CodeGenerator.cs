using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
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

        public static string GetCodeForState(ElementSave container, StateSave stateSave, VisualApi visualApi)
        {
            VariableSave[] variablesToConsider = stateSave.Variables
                // make "Parent" first
                .Where(item => item.GetRootName() != "Parent")
                .ToArray();

            string last = null;

            var stringBuilder = new StringBuilder();
            foreach (var variable in variablesToConsider)
            {
                InstanceSave instance = null;

                var instanceName = variable.SourceObject;

                if (instanceName != null)
                {
                    instance = container.GetInstance(instanceName);
                }

                if(string.IsNullOrWhiteSpace(last) == false && last != instanceName)
                {
                    stringBuilder.AppendLine();
                }

                if(instance != null)
                {
                    var codeLine = GetCodeLine(instance, variable, visualApi);
                    stringBuilder.AppendLine(codeLine);

                    var suffixCodeLine = GetSuffixCodeLine(instance, variable, visualApi);
                    if (!string.IsNullOrEmpty(suffixCodeLine))
                    {
                        stringBuilder.AppendLine(suffixCodeLine);
                    }
                }
                last = instanceName;
            }

            var code = stringBuilder.ToString();
            return code;
        }

        public static string GetCodeForElement(ElementSave element, VisualApi visualApi)
        {
            var stringBuilder = new StringBuilder();


            foreach(var instance in element.Instances)
            {
                var instanceCode = GetCodeForInstance(instance, visualApi);
                stringBuilder.AppendLine(instanceCode);
                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }

        public static string GetCodeForInstance(InstanceSave instance, VisualApi visualApi)
        {
            // use default state? Or current state? Let's start with default:

            VariableSave[] variablesToConsider = GetVariablesToConsider(instance)
                // make "Parent" first
                .OrderBy(item => item.GetRootName() != "Parent")
                .ToArray();

            var stringBuilder = new StringBuilder();

            var strippedType = instance.BaseType;
            if(strippedType.Contains("/"))
            {
                strippedType = strippedType.Substring(strippedType.LastIndexOf("/") + 1);
            }

            string suffix = visualApi == VisualApi.Gum ? "Runtime" : "";
            var className = $"{strippedType}{suffix}";

            stringBuilder.AppendLine($"{className} {instance.Name};");
            stringBuilder.AppendLine($"{instance.Name} = new {className}();");

            foreach (var variable in variablesToConsider)
            {
                var codeLine = GetCodeLine(instance, variable, visualApi);
                stringBuilder.AppendLine(codeLine);

                var suffixCodeLine = GetSuffixCodeLine(instance, variable, visualApi);
                if(!string.IsNullOrEmpty(suffixCodeLine))
                {
                    stringBuilder.AppendLine(suffixCodeLine);
                }
            }

            var code = stringBuilder.ToString();
            return code;
        }

        private static string GetSuffixCodeLine(InstanceSave instance, VariableSave variable, VisualApi visualApi)
        {
            if(visualApi == VisualApi.XamarinForms)
            {
                var rootName = variable.GetRootName();

                switch(rootName)
                {
                    case "Width": return $"{instance.Name}.HorizontalOptions = LayoutOptions.Start;";
                    case "Height": return $"{instance.Name}.VerticalOptions = LayoutOptions.Start;";
                }
            }

            return null;
        }

        private static string GetCodeLine(InstanceSave instance, VariableSave variable, VisualApi visualApi)
        {
            if (visualApi == VisualApi.Gum)
            {
                var fullLineReplacement = TryGetFullGumLineReplacement(instance, variable);

                if(!string.IsNullOrEmpty(fullLineReplacement))
                {
                    return fullLineReplacement;
                }
                else
                {
                    return $"{instance.Name}.{GetGumVariableName(variable)} = {VariableValueToGumCodeValue(variable)};";
                }

            }
            else // xamarin forms
            {
                return $"{instance.Name}.{GetXamarinFormsVariableName(variable)} = {VariableValueToXamarinFormsCodeValue(variable)};";

            }
        }

        private static string TryGetFullGumLineReplacement(InstanceSave instance, VariableSave variable)
        {
            var rootName = variable.GetRootName();
            if (rootName == "Parent")
            {
                return $"{variable.Value}.Children.Add({instance.Name});";
            }
            return null;
        }

        private static string VariableValueToGumCodeValue(VariableSave variable)
        {
            if(variable.Value is float asFloat)
            {
                return asFloat.ToString(CultureInfo.InvariantCulture) + "f";
            }
            else if(variable.Value is string)
            {
                if(variable.GetRootName() == "Parent")
                {
                    return variable.Value.ToString();
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

        private static string VariableValueToXamarinFormsCodeValue(VariableSave variable)
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
                else
                {
                    return "\"" + variable.Value + "\"";
                }
            }
            else if (variable.Value.GetType().IsEnum)
            {
                var type = variable.Value.GetType();
                if (type == typeof(PositionUnitType))
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

        private static object GetGumVariableName(VariableSave variable)
        {
            return variable.GetRootName().Replace(" ", "");
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

                default: return rootName;
            }
        }

        private static VariableSave[] GetVariablesToConsider(InstanceSave instance)
        {
            var defaultState = SelectedState.Self.SelectedElement.DefaultState;
            var variablesToConsider = defaultState.Variables
                .Where(item =>
                {
                    return
                        item.Value != null &&
                        item.SetsValue &&
                        item.SourceObject == instance.Name;
                })
                .ToArray();
            return variablesToConsider;
        }
    }
}
