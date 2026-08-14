using System.Collections.Generic;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// The values captured by a Variables tab category copy, held in memory by
/// <see cref="VariableCategoryCopyPasteService"/> rather than on the system clipboard (mirroring
/// <c>CopyPasteLogic.CopiedData</c>).
/// </summary>
public class CopiedVariableCategory
{
    /// <summary>The name of the category the values came from, used to label the paste menu item.</summary>
    public string CategoryName { get; }

    /// <summary>The captured values, in the order they appeared in the source category.</summary>
    public IReadOnlyList<CopiedVariableValue> Values { get; }

    public CopiedVariableCategory(string categoryName, IReadOnlyList<CopiedVariableValue> values)
    {
        CategoryName = categoryName;
        Values = values;
    }
}

/// <summary>A single variable name/value pair captured by a category copy.</summary>
public class CopiedVariableValue
{
    public string RootVariableName { get; }

    public object Value { get; }

    public CopiedVariableValue(string rootVariableName, object value)
    {
        RootVariableName = rootVariableName;
        Value = value;
    }
}
