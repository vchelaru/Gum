using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Gum.Undo;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <inheritdoc cref="IVariableCategoryCopyPasteService"/>
public class VariableCategoryCopyPasteService : IVariableCategoryCopyPasteService
{
    /// <summary>
    /// Variables that identify or gate an object rather than describe its appearance. Copying a whole
    /// category must never rename the target, retype it, lock it, reparent it, or overwrite its
    /// reference list.
    /// </summary>
    private static readonly HashSet<string> NeverCopiedVariableNames = new()
    {
        "Name",
        "BaseType",
        "DefaultChildContainer",
        "Locked",
        "Parent",
        "VariableReferences"
    };

    private static readonly HashSet<Type> NumericTypes = new()
    {
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)
    };

    private enum PasteOutcome
    {
        Skipped,
        AlreadyMatched,
        Written
    }

    private readonly IUndoManager _undoManager;

    /// <inheritdoc/>
    public CopiedVariableCategory? CopiedCategory { get; private set; }

    public VariableCategoryCopyPasteService(IUndoManager undoManager)
    {
        _undoManager = undoManager;
    }

    /// <inheritdoc/>
    public CopiedVariableCategory Copy(string categoryName, IEnumerable<IVariableCategoryRow> rows)
    {
        List<CopiedVariableValue> values = new List<CopiedVariableValue>();
        List<string> indeterminate = new List<string>();

        foreach (IVariableCategoryRow row in rows)
        {
            if (NeverCopiedVariableNames.Contains(row.RootVariableName))
            {
                continue;
            }

            // A multi-selection whose instances disagree has no single value to capture. Report it rather
            // than letting it look like the copy silently covered the whole category.
            if (row.IsIndeterminate)
            {
                indeterminate.Add(row.RootVariableName);
                continue;
            }

            object? value = row.Value;

            // A null row has no value to stamp onto the target. Writing null would author an explicit
            // null rather than restoring inheritance (which is what "Make Default" does), so skip it.
            if (value == null)
            {
                continue;
            }

            // Copying holds the value by reference, so handing a list to a second element would leave both
            // sharing one instance and editing one would silently change the other.
            if (value is System.Collections.IList)
            {
                continue;
            }

            values.Add(new CopiedVariableValue(row.RootVariableName, value));
        }

        CopiedCategory = new CopiedVariableCategory(categoryName, values, indeterminate);
        return CopiedCategory;
    }

    /// <inheritdoc/>
    public VariableCategoryPasteResult Paste(string targetCategoryName, IEnumerable<IVariableCategoryRow> targetRows)
    {
        List<string> applied = new List<string>();
        List<string> alreadyMatched = new List<string>();
        List<string> skipped = new List<string>();

        if (CopiedCategory == null || CopiedCategory.CategoryName != targetCategoryName)
        {
            return new VariableCategoryPasteResult(applied, alreadyMatched, skipped);
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

        // Units before values: setting XUnits/WidthUnits/... can convert the target's current X/Width to
        // preserve on-screen position ("convert variables on unit type change"), so a value written before
        // its unit gets converted away from what was copied. Applying units first makes the conversion act
        // on the target's old value, and the copied value then lands verbatim. OrderBy is stable, so the
        // original category order is kept within each group.
        IEnumerable<CopiedVariableValue> orderedValues = CopiedCategory.Values
            .OrderBy(item => item.RootVariableName.EndsWith("Units", StringComparison.Ordinal) ? 0 : 1);

        // One lock across every write, so the whole group is a single undo. Nested locks taken by the
        // multi-select rows underneath are harmless: recording only resumes once the last lock is gone.
        using (_undoManager.RequestLock())
        {
            foreach (CopiedVariableValue copiedValue in orderedValues)
            {
                switch (TryApply(copiedValue, rowsByName))
                {
                    case PasteOutcome.Written:
                        applied.Add(copiedValue.RootVariableName);
                        break;
                    case PasteOutcome.AlreadyMatched:
                        alreadyMatched.Add(copiedValue.RootVariableName);
                        break;
                    default:
                        skipped.Add(copiedValue.RootVariableName);
                        break;
                }
            }
        }

        return new VariableCategoryPasteResult(applied, alreadyMatched, skipped);
    }

    private static PasteOutcome TryApply(CopiedVariableValue copiedValue, Dictionary<string, IVariableCategoryRow> rowsByName)
    {
        if (!rowsByName.TryGetValue(copiedValue.RootVariableName, out IVariableCategoryRow? row))
        {
            return PasteOutcome.Skipped;
        }

        if (row.IsReadOnly || row.IsAssignedByReference)
        {
            return PasteOutcome.Skipped;
        }

        if (!TryResolveValueForTarget(copiedValue.Value, row.ValueType, out object valueToWrite))
        {
            return PasteOutcome.Skipped;
        }

        // The target already shows this value. Writing it anyway would author an inherited value explicitly
        // onto the target for no gain, so leave it alone. A multi-select row whose instances disagree
        // reports null (indeterminate), which never matches, so a paste onto it still unifies the selection.
        if (Equals(row.Value, valueToWrite))
        {
            return PasteOutcome.AlreadyMatched;
        }

        return row.TrySetValue(valueToWrite) ? PasteOutcome.Written : PasteOutcome.Skipped;
    }

    /// <summary>
    /// Decides what to write to the target row, if anything. Same-named variables can still differ in type
    /// between elements (for example a Font that is a system font name on one object and a file on
    /// another); the write path stores the value raw, so a mistyped box would fail later casts. Numeric
    /// mismatches (an int 36 pasted onto a float row) are converted rather than skipped; any other
    /// mismatch is rejected. A row that cannot report its type is written as-is and left to reject the
    /// value itself.
    /// </summary>
    private static bool TryResolveValueForTarget(object copiedValue, Type? targetType, out object valueToWrite)
    {
        valueToWrite = copiedValue;

        if (targetType == null)
        {
            return true;
        }

        // Nullable targets are common (int? MaxLettersToShow, float? MinWidth) and a copied value is never
        // null here, so compare against the underlying type - IsInstanceOfType on Nullable<T> is always
        // false for a boxed T.
        Type effectiveTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (effectiveTargetType.IsInstanceOfType(copiedValue))
        {
            return true;
        }

        if (NumericTypes.Contains(effectiveTargetType) && NumericTypes.Contains(copiedValue.GetType()))
        {
            try
            {
                valueToWrite = Convert.ChangeType(copiedValue, effectiveTargetType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        return false;
    }
}
