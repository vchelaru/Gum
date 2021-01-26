using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumDataTypes.Variables
{
    public interface IVariableFinder
    {
        T GetValue<T>(string variableName);
    }
}
