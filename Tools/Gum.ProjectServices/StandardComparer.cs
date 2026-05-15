using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using ToolsUtilities;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class StandardComparer : IStandardComparer
{
    /// <inheritdoc/>
    public StandardComparisonResult Compare(StandardElementSave source, StandardElementSave destination)
    {
        StandardComparisonResult result = new StandardComparisonResult();

        // Category name sets — same logic as the historic tool-side StandardsDiffer.
        List<string> sourceCategoryNames = source.Categories
            .Select(c => c.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
        List<string> destCategoryNames = destination.Categories
            .Select(c => c.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();

        result.CategoryNamesDiffer = !sourceCategoryNames.SequenceEqual(destCategoryNames);
        result.CategoryNamesOnlyInSource.AddRange(sourceCategoryNames.Except(destCategoryNames));
        result.CategoryNamesOnlyInDestination.AddRange(destCategoryNames.Except(sourceCategoryNames));

        // DefaultState XML compare — also same logic as StandardsDiffer.
        StateSave? sourceDefault = source.DefaultState;
        StateSave? destDefault = destination.DefaultState;

        if (sourceDefault == null && destDefault == null)
        {
            result.DefaultStateXmlDiffers = false;
        }
        else if (sourceDefault == null || destDefault == null)
        {
            result.DefaultStateXmlDiffers = true;
            ComputeVariableDifferences(sourceDefault, destDefault, result.VariableDifferences);
        }
        else
        {
            StateSave sourceClone = sourceDefault.Clone();
            StateSave destClone = destDefault.Clone();
            sourceClone.Variables.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            destClone.Variables.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            FileManager.XmlSerialize(sourceClone, out string sourceSerialized);
            FileManager.XmlSerialize(destClone, out string destSerialized);

            result.DefaultStateXmlDiffers = sourceSerialized != destSerialized;
            if (result.DefaultStateXmlDiffers)
            {
                ComputeVariableDifferences(sourceClone, destClone, result.VariableDifferences);
            }
        }

        result.HasDifferences = result.CategoryNamesDiffer || result.DefaultStateXmlDiffers;
        return result;
    }

    /// <summary>
    /// Walks two DefaultState <see cref="StateSave"/>s, comparing each variable by name and
    /// reporting one <see cref="StandardVariableDiff"/> per variable that differs. Includes
    /// non-<c>Value</c> fields (Type, Category, SetsValue, IsFile, IsFont,
    /// IsHiddenInPropertyGrid, ExposedAsName) via <see cref="StandardVariableDiff.ChangedFields"/>.
    /// </summary>
    private static void ComputeVariableDifferences(
        StateSave? sourceState,
        StateSave? destState,
        List<StandardVariableDiff> diffs)
    {
        Dictionary<string, VariableSave> sourceVars = (sourceState?.Variables ?? new List<VariableSave>())
            .GroupBy(v => v.Name).ToDictionary(g => g.Key, g => g.First());
        Dictionary<string, VariableSave> destVars = (destState?.Variables ?? new List<VariableSave>())
            .GroupBy(v => v.Name).ToDictionary(g => g.Key, g => g.First());

        foreach (string name in sourceVars.Keys.Union(destVars.Keys).OrderBy(n => n, StringComparer.Ordinal))
        {
            bool inSource = sourceVars.TryGetValue(name, out VariableSave? sourceVar);
            bool inDest = destVars.TryGetValue(name, out VariableSave? destVar);

            if (inSource && inDest)
            {
                StandardVariableDiff? diff = CompareVariables(sourceVar!, destVar!);
                if (diff != null)
                {
                    diff.VariableName = name;
                    diffs.Add(diff);
                }
            }
            else if (inSource)
            {
                diffs.Add(new StandardVariableDiff
                {
                    VariableName = name,
                    Kind = StandardVariableDiffKind.AddedInProject,
                    DefaultValue = "(absent)",
                    ProjectValue = FormatValue(sourceVar!.Value)
                });
            }
            else
            {
                diffs.Add(new StandardVariableDiff
                {
                    VariableName = name,
                    Kind = StandardVariableDiffKind.RemovedFromProject,
                    DefaultValue = FormatValue(destVar!.Value),
                    ProjectValue = "(absent)"
                });
            }
        }

        // VariableLists — same dictionary-style walk.
        Dictionary<string, VariableListSave> sourceLists = (sourceState?.VariableLists ?? new List<VariableListSave>())
            .GroupBy(v => v.Name).ToDictionary(g => g.Key, g => g.First());
        Dictionary<string, VariableListSave> destLists = (destState?.VariableLists ?? new List<VariableListSave>())
            .GroupBy(v => v.Name).ToDictionary(g => g.Key, g => g.First());

        foreach (string name in sourceLists.Keys.Union(destLists.Keys).OrderBy(n => n, StringComparer.Ordinal))
        {
            bool inSource = sourceLists.TryGetValue(name, out VariableListSave? sourceList);
            bool inDest = destLists.TryGetValue(name, out VariableListSave? destList);

            string sourceXml = inSource ? SerializeList(sourceList!) : string.Empty;
            string destXml = inDest ? SerializeList(destList!) : string.Empty;

            if (sourceXml == destXml)
            {
                continue;
            }

            StandardVariableDiffKind kind =
                !inDest ? StandardVariableDiffKind.AddedInProject :
                !inSource ? StandardVariableDiffKind.RemovedFromProject :
                StandardVariableDiffKind.Changed;

            diffs.Add(new StandardVariableDiff
            {
                VariableName = name + " (VariableList)",
                Kind = kind,
                ProjectValue = inSource ? Summarize(sourceList!) : "(absent)",
                DefaultValue = inDest ? Summarize(destList!) : "(absent)"
            });
        }
    }

    private static StandardVariableDiff? CompareVariables(VariableSave source, VariableSave dest)
    {
        StandardVariableDiff diff = new StandardVariableDiff
        {
            Kind = StandardVariableDiffKind.Changed,
            ProjectValue = FormatValue(source.Value),
            DefaultValue = FormatValue(dest.Value)
        };

        bool any = false;

        if (!AreValuesEqual(source.Value, dest.Value))
        {
            any = true;
            // ProjectValue/DefaultValue already reflect the value change.
        }

        TryAddFieldDiff(diff, "Type", source.Type, dest.Type, ref any);
        TryAddFieldDiff(diff, "Category", source.Category, dest.Category, ref any);
        TryAddFieldDiff(diff, "SetsValue", source.SetsValue, dest.SetsValue, ref any);
        TryAddFieldDiff(diff, "IsFile", source.IsFile, dest.IsFile, ref any);
        TryAddFieldDiff(diff, "IsFont", source.IsFont, dest.IsFont, ref any);
        TryAddFieldDiff(diff, "IsHiddenInPropertyGrid",
            source.IsHiddenInPropertyGrid, dest.IsHiddenInPropertyGrid, ref any);
        TryAddFieldDiff(diff, "ExposedAsName", source.ExposedAsName, dest.ExposedAsName, ref any);

        return any ? diff : null;
    }

    private static void TryAddFieldDiff(
        StandardVariableDiff diff, string fieldName, object? source, object? dest, ref bool any)
    {
        string sourceStr = FormatValue(source);
        string destStr = FormatValue(dest);
        if (sourceStr == destStr)
        {
            return;
        }
        any = true;
        diff.ChangedFields.Add(new VariableFieldDiff
        {
            FieldName = fieldName,
            ProjectValue = sourceStr,
            DefaultValue = destStr
        });
    }

    private static string SerializeList(VariableListSave list)
    {
        // Use the runtime XmlSerializer for the concrete type so subclasses like
        // VariableListSaveOfString round-trip with their xsi:type attribute intact.
        var serializer = FileManager.GetXmlSerializer(list.GetType());
        using var writer = new System.IO.StringWriter();
        serializer.Serialize(writer, list);
        return writer.ToString();
    }

    private static string Summarize(VariableListSave list)
    {
        // Short, human-readable hint — the full XML is preserved on disk in the .gutx.
        int count = 0;
        if (list.ValueAsIList != null)
        {
            foreach (object? _ in list.ValueAsIList)
            {
                count++;
            }
        }
        return $"{list.Type}[{count}]";
    }

    private static bool AreValuesEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        if (a is float || a is double || b is float || b is double)
        {
            return FormatValue(a) == FormatValue(b);
        }
        return a.Equals(b);
    }

    private static string FormatValue(object? value)
    {
        if (value == null) return "(unset)";
        if (value is string s) return s.Length == 0 ? "(empty)" : s;
        if (value is bool b) return b ? "True" : "False";
        if (value is IFormattable f) return f.ToString(null, CultureInfo.InvariantCulture);
        return value.ToString() ?? "(unset)";
    }
}
