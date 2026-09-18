using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// What the tool hands to a running GumPreview through the selection file: the element to show,
/// the state to put it in, the sibling orderer to render with, and whether to raise the window.
/// One <c>key=value</c> line per field. Compiled into both the tool (EditorTabPlugin.Core) and
/// GumPreview (as a linked file) so the two sides never disagree on the format.
/// </summary>
public class PreviewSelectionMessage
{
    private const string ElementKey = "element";
    private const string CategoryKey = "category";
    private const string StateKey = "state";
    private const string SortByBatchKeyKey = "sortByBatchKey";
    private const string ActivateKey = "activate";
    // Last line of every message, so a reader that catches the file mid-write can tell.
    private const string EndLine = "end=true";

    /// <summary>The screen or component to show.</summary>
    public string ElementName { get; }

    /// <summary>The category of <see cref="StateName"/>, or null for an uncategorized state.</summary>
    public string? CategoryName { get; set; }

    /// <summary>The state to apply on top of the default state, or null to show the default state only.</summary>
    public string? StateName { get; set; }

    /// <summary>Whether the tool renders with <c>BatchKeyGroupedOrderer</c> rather than <c>HierarchicalOrderer</c>.</summary>
    public bool SortByBatchKey { get; set; }

    /// <summary>Whether GumPreview should also raise its window (an explicit re-click of Preview).</summary>
    public bool Activate { get; set; }

    public PreviewSelectionMessage(string elementName)
    {
        ElementName = elementName;
    }

    /// <summary>Serializes to the selection-file text that <see cref="TryParse"/> reads back.</summary>
    public string Serialize()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(ElementKey).Append('=').Append(ElementName).Append('\n');
        if (!string.IsNullOrEmpty(CategoryName))
        {
            builder.Append(CategoryKey).Append('=').Append(CategoryName).Append('\n');
        }
        if (!string.IsNullOrEmpty(StateName))
        {
            builder.Append(StateKey).Append('=').Append(StateName).Append('\n');
        }
        if (SortByBatchKey)
        {
            builder.Append(SortByBatchKeyKey).Append("=true\n");
        }
        if (Activate)
        {
            builder.Append(ActivateKey).Append("=true\n");
        }
        builder.Append(EndLine).Append('\n');
        return builder.ToString();
    }

    /// <summary>
    /// Parses selection-file lines; null when no element line is present or the end line is
    /// missing (the file is still being written).
    /// </summary>
    public static PreviewSelectionMessage? TryParse(IEnumerable<string> lines)
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        bool isComplete = false;
        foreach (string line in lines)
        {
            if (line.Trim() == EndLine)
            {
                isComplete = true;
                break;
            }
            int separatorIndex = line.IndexOf('=');
            if (separatorIndex > 0)
            {
                values[line.Substring(0, separatorIndex).Trim()] = line.Substring(separatorIndex + 1).Trim();
            }
        }

        if (!isComplete || !values.TryGetValue(ElementKey, out string? elementName) || string.IsNullOrEmpty(elementName))
        {
            return null;
        }

        return new PreviewSelectionMessage(elementName)
        {
            CategoryName = values.TryGetValue(CategoryKey, out string? category) ? category : null,
            StateName = values.TryGetValue(StateKey, out string? state) ? state : null,
            SortByBatchKey = IsTrue(values, SortByBatchKeyKey),
            Activate = IsTrue(values, ActivateKey),
        };
    }

    /// <summary>
    /// Whether <paramref name="other"/> shows the same element in the same state - the part of the
    /// message that requires rebuilding the preview's root when it changes.
    /// </summary>
    public bool HasSameSelection(PreviewSelectionMessage other) =>
        ElementName == other.ElementName && CategoryName == other.CategoryName && StateName == other.StateName;

    /// <summary>
    /// Finds the state this message names on <paramref name="element"/>: a state in
    /// <see cref="CategoryName"/> when set, otherwise an uncategorized one. Null when no state is
    /// named or it doesn't exist.
    /// </summary>
    public StateSave? FindState(ElementSave element)
    {
        if (string.IsNullOrEmpty(StateName))
        {
            return null;
        }

        IEnumerable<StateSave> candidates = string.IsNullOrEmpty(CategoryName)
            ? element.States
            : element.Categories.Where(category => category.Name == CategoryName).SelectMany(category => category.States);
        return candidates.FirstOrDefault(state => state.Name == StateName);
    }

    private static bool IsTrue(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out string? value) && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
