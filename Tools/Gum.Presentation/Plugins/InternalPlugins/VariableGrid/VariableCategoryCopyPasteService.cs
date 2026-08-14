using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Undo;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <inheritdoc cref="IVariableCategoryCopyPasteService"/>
public class VariableCategoryCopyPasteService : IVariableCategoryCopyPasteService
{
    /// <summary>
    /// Variables that identify or gate an object rather than describe its appearance. Copying a whole
    /// category must never rename the target, retype it, lock it, or overwrite its reference list.
    /// </summary>
    private static readonly HashSet<string> NeverCopiedVariableNames = new()
    {
        "Name",
        "BaseType",
        "Locked",
        "Parent",
        "VariableReferences"
    };

    private readonly IUndoManager _undoManager;

    public CopiedVariableCategory? CopiedCategory { get; private set; }

    public VariableCategoryCopyPasteService(IUndoManager undoManager)
    {
        _undoManager = undoManager;
    }

    public void Copy(string categoryName, IEnumerable<IVariableCategoryRow> rows)
    {
        List<CopiedVariableValue> values = new List<CopiedVariableValue>();

        foreach (IVariableCategoryRow row in rows)
        {
            if (NeverCopiedVariableNames.Contains(row.RootVariableName))
            {
                continue;
            }

            // A null row has no value to stamp onto the target. Writing null would author an explicit
            // null rather than restoring inheritance (which is what "Make Default" does), so skip it.
            if (row.Value == null)
            {
                continue;
            }

            // Copying holds the value by reference, so handing a list to a second element would leave both
            // sharing one instance and editing one would silently change the other.
            if (row.Value is System.Collections.IList)
            {
                continue;
            }

            values.Add(new CopiedVariableValue(row.RootVariableName, row.Value));
        }

        CopiedCategory = new CopiedVariableCategory(categoryName, values);
    }

    public VariableCategoryPasteResult Paste(IEnumerable<IVariableCategoryRow> targetRows)
    {
        List<string> applied = new List<string>();
        List<string> skipped = new List<string>();

        if (CopiedCategory == null)
        {
            return new VariableCategoryPasteResult(applied, skipped);
        }

        Dictionary<string, IVariableCategoryRow> rowsByName = new Dictionary<string, IVariableCategoryRow>();
        foreach (IVariableCategoryRow row in targetRows)
        {
            // A category should not contain the same variable twice, but if it does the first row wins
            // rather than throwing mid-paste.
            if (!rowsByName.ContainsKey(row.RootVariableName))
            {
                rowsByName.Add(row.RootVariableName, row);
            }
        }

        // One lock across every write, so the whole group is a single undo. Nested locks taken by the
        // multi-select rows underneath are harmless: recording only resumes once the last lock is gone.
        using (_undoManager.RequestLock())
        {
            foreach (CopiedVariableValue copiedValue in CopiedCategory.Values)
            {
                if (TryApply(copiedValue, rowsByName))
                {
                    applied.Add(copiedValue.RootVariableName);
                }
                else
                {
                    skipped.Add(copiedValue.RootVariableName);
                }
            }
        }

        return new VariableCategoryPasteResult(applied, skipped);
    }

    private static bool TryApply(CopiedVariableValue copiedValue, Dictionary<string, IVariableCategoryRow> rowsByName)
    {
        if (!rowsByName.TryGetValue(copiedValue.RootVariableName, out IVariableCategoryRow? row))
        {
            return false;
        }

        if (row.IsReadOnly || row.IsAssignedByReference)
        {
            return false;
        }

        if (!IsTypeCompatible(copiedValue.Value, row.ValueType))
        {
            return false;
        }

        // The target already shows this value. Writing it anyway would author an inherited value explicitly
        // onto the target for no gain, so leave it alone and report it as matched.
        if (Equals(row.Value, copiedValue.Value))
        {
            return true;
        }

        return row.TrySetValue(copiedValue.Value);
    }

    /// <summary>
    /// Whether a copied value can stand in for the target row. Same-named variables can still differ in
    /// type between elements (for example a Font that is a system font name on one object and a file on
    /// another). A row that cannot report its type is allowed through and left to reject the value itself.
    /// </summary>
    private static bool IsTypeCompatible(object copiedValue, Type? targetValueType)
    {
        if (targetValueType == null)
        {
            return true;
        }

        return targetValueType.IsInstanceOfType(copiedValue);
    }
}
