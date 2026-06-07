using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Wireframe;

/// <summary>
/// Builds Gum save data (states/variables) from a live <see cref="GraphicalUiElement"/> tree, for the
/// runtime inspector snapshot feature (issue #3070). Which variables exist for a given node is decided
/// by the standard-element catalog (what Gum understands), and each value is read from the live element
/// via <see cref="GraphicalUiElementPropertyReadExtensions.TryGetProperty"/>.
/// </summary>
public interface IRuntimeSnapshotSerializer
{
    /// <summary>
    /// Resolves the Gum standard-element type name (e.g. "Container", "Text") for a live element by
    /// walking its runtime type hierarchy, or null if no standard type applies.
    /// </summary>
    string? GetStandardTypeName(GraphicalUiElement element);

    /// <summary>
    /// Creates a state holding the element's current values for every variable in its standard type's
    /// catalog. The result is unshaken — redundant (equal-to-default) values are not yet removed.
    /// </summary>
    StateSave CreateStateForNode(GraphicalUiElement element, string stateName);

    /// <summary>
    /// Builds a flattened <see cref="ScreenSave"/> snapshot of the live tree rooted at
    /// <paramref name="root"/>. Each descendant becomes a standard-element <see cref="InstanceSave"/>; the
    /// screen's default state holds each instance's values as instance-qualified variables, and
    /// child/parent structure is captured via the qualified "Parent" variable. The root itself maps to the
    /// screen, not to an instance.
    /// </summary>
    ScreenSave CreateScreenSave(GraphicalUiElement root, string screenName);
}

/// <inheritdoc cref="IRuntimeSnapshotSerializer" />
public class RuntimeSnapshotSerializer : IRuntimeSnapshotSerializer
{
    private const string RuntimeSuffix = "Runtime";

    private readonly IReadOnlyDictionary<string, StateSave> _defaultStates;

    /// <summary>
    /// Creates a serializer that reads runtime values against the supplied standard-element catalog.
    /// </summary>
    /// <param name="defaultStates">
    /// The standard-element default states keyed by type name — the authority on which variables Gum
    /// understands per type. Typically <c>StandardElementsManager.Self.DefaultStates</c>.
    /// </param>
    public RuntimeSnapshotSerializer(IReadOnlyDictionary<string, StateSave> defaultStates)
    {
        _defaultStates = defaultStates;
    }

    /// <inheritdoc />
    public string? GetStandardTypeName(GraphicalUiElement element)
    {
        // Standard runtimes follow the "<StandardName>Runtime" convention (ContainerRuntime ->
        // "Container"). Walking the base chain lets custom subclasses resolve to their nearest
        // standard ancestor; matching against the catalog keeps the result grounded in what Gum
        // actually understands rather than trusting the type name blindly.
        Type? type = element.GetType();
        while (type != null)
        {
            string candidate = StripRuntimeSuffix(type.Name);
            if (_defaultStates.ContainsKey(candidate))
            {
                return candidate;
            }
            type = type.BaseType;
        }
        return null;
    }

    /// <inheritdoc />
    public StateSave CreateStateForNode(GraphicalUiElement element, string stateName)
    {
        StateSave state = new StateSave { Name = stateName };

        string? typeName = GetStandardTypeName(element);
        if (typeName != null && _defaultStates.TryGetValue(typeName, out StateSave? defaultState))
        {
            foreach (VariableSave defaultVariable in defaultState.Variables)
            {
                // The catalog defines the name/type/category; the live element supplies the value.
                // Variables the element cannot read (no base-set case and no reflectable property)
                // are skipped rather than emitted with a wrong value.
                if (element.TryGetProperty(defaultVariable.Name, out object? value))
                {
                    state.Variables.Add(new VariableSave
                    {
                        Name = defaultVariable.Name,
                        Type = defaultVariable.Type,
                        Value = value,
                        SetsValue = true,
                        Category = defaultVariable.Category,
                    });
                }
            }
        }

        return state;
    }

    /// <inheritdoc />
    public ScreenSave CreateScreenSave(GraphicalUiElement root, string screenName)
    {
        ScreenSave screen = new ScreenSave { Name = screenName };
        StateSave defaultState = new StateSave { Name = "Default" };
        screen.States.Add(defaultState);

        HashSet<string> usedNames = new HashSet<string>();
        foreach (GraphicalUiElement child in root.Children)
        {
            AddInstanceRecursive(child, parentInstanceName: null, screen, defaultState, usedNames);
        }

        return screen;
    }

    private void AddInstanceRecursive(GraphicalUiElement element, string? parentInstanceName,
        ScreenSave screen, StateSave defaultState, HashSet<string> usedNames)
    {
        string typeName = GetStandardTypeName(element) ?? "Container";
        string instanceName = GenerateUniqueName(element, typeName, usedNames);

        screen.Instances.Add(new InstanceSave { Name = instanceName, BaseType = typeName });

        // The node's catalog values become instance-qualified variables in the screen's default state.
        StateSave nodeState = CreateStateForNode(element, "Default");
        foreach (VariableSave variable in nodeState.Variables)
        {
            defaultState.Variables.Add(new VariableSave
            {
                Name = instanceName + "." + variable.Name,
                Type = variable.Type,
                Value = variable.Value,
                SetsValue = true,
                Category = variable.Category,
            });
        }

        // Non-top-level instances record their parent; top-level instances are children of the screen.
        if (parentInstanceName != null)
        {
            defaultState.Variables.Add(new VariableSave
            {
                Name = instanceName + ".Parent",
                Type = "string",
                Value = parentInstanceName,
                SetsValue = true,
            });
        }

        foreach (GraphicalUiElement child in element.Children)
        {
            AddInstanceRecursive(child, instanceName, screen, defaultState, usedNames);
        }
    }

    private static string GenerateUniqueName(GraphicalUiElement element, string typeName, HashSet<string> usedNames)
    {
        string baseName = string.IsNullOrEmpty(element.Name) ? typeName : element.Name;
        string candidate = baseName;
        int suffix = 1;
        while (!usedNames.Add(candidate))
        {
            candidate = baseName + suffix;
            suffix++;
        }
        return candidate;
    }

    private static string StripRuntimeSuffix(string typeName)
    {
        if (typeName.Length > RuntimeSuffix.Length && typeName.EndsWith(RuntimeSuffix, StringComparison.Ordinal))
        {
            return typeName.Substring(0, typeName.Length - RuntimeSuffix.Length);
        }
        return typeName;
    }
}
