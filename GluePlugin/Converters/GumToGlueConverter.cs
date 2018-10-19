using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GluePlugin.Converters
{
    public class GumToGlueConverter : Singleton<GumToGlueConverter>
    {
        public string ConvertVariableName(string variableName)
        {
            var convertedName = variableName;

            return convertedName;
        }

        public object ConvertVariableValue(string variableName, object variableValue)
        {
            var convertedValue = variableValue;

            return convertedValue;
        }
    }
}
