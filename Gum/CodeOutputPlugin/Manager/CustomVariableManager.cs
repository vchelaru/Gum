using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.Manager;

internal class CustomVariableManager
{
    public static void HandleAddAndRemoveVariablesForType(string type, StateSave stateSave)
    {
        // this controls whether IsXamarinFormsControl and IsOverride variables exist.
        // These were added to support maui/xamforms which are currently not supported, so
        // we are no longer generating these:

        stateSave.Variables.RemoveAll(item => 
            item.Name == "IsXamarinFormsControl" ||
            item.Name == "IsOverrideInCodeGen"
        );
    }
}
