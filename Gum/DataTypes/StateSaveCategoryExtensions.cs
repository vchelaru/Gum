using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Managers;
using System.Collections;
using ToolsUtilities;
using System.CodeDom;
using System.Reflection.Emit;
//using Gum.Wireframe;


//using Gum.Reflection;


namespace Gum.DataTypes.Variables;

public static class StateSaveCategoryExtensions
{
    public static void SetValues(this StateSaveCategory category, string name, object value)
    {
        foreach(var state in category.States)
        {
            state.SetValue(name, value);
        }
    }

    public static void RemoveValues(this StateSaveCategory category, string name)
    {
        foreach(var state in category.States)
        {
            state.RemoveValue(name);
        }
    }

    public static void ResetAllStates(this StateSaveCategory category)
    {
        foreach(var state in category.States)
        {
            state.Clear();
        }
    }
}
