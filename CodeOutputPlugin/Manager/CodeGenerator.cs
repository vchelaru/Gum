using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.Manager
{
    public static class CodeGenerator
    {
        public static string GetCodeForInstance(InstanceSave instance)
        {
            // use default state? Or current state? Let's start with default:

            VariableSave[] variablesToConsider = GetVariablesToConsider(instance);

            var stringBuilder = new StringBuilder();



            foreach (var variable in variablesToConsider)
            {
                stringBuilder.AppendLine($"{instance.Name}.{GetVariableName(variable)} = {VariableValueToCodeValue(variable)};");
            }

            var code = stringBuilder.ToString();
            return code;
        }

        private static string VariableValueToCodeValue(VariableSave variable)
        {
            if(variable.Value is float asFloat)
            {
                return asFloat.ToString(CultureInfo.InvariantCulture) + "f";
            }
            else if(variable.Value is string)
            {
                return "\"" + variable.Value + "\"";
            }
            else if(variable.Value.GetType().IsEnum)
            {

                return variable.Value.GetType().Name + "." + variable.Value.ToString();
            }
            else
            {
                return variable.Value?.ToString();
            }
        }

        private static object GetVariableName(VariableSave variable)
        {
            return variable.GetRootName().Replace(" ", "");
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
