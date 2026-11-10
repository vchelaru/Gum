using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Resolves <c>FindAncestor&lt;BehaviorName&gt;()</c> in a behavior's tool-only reference line to the name of the
/// nearest ancestor instance whose component, or a base component, has that behavior (#7827). Ancestors are
/// found by walking <c>Parent</c> values, so intermediate containers and slot paths
/// (<c>TabControl1.TabStripInstance</c>) count, and the owner may be declared by a base element.
/// </summary>
internal sealed class BehaviorAncestorFinder
{
    // Group 1: a string literal, left untouched. Group 2: the behavior name of a FindAncestor<Name>() call,
    // with or without the @ prefix. A behavior name must be an identifier; anything else never matches, so the
    // line is left as written and evaluates to nothing.
    private static readonly Regex FindAncestorCall =
        new Regex(@"(""(?:[^""\\]|\\.)*"")|@?FindAncestor\s*<\s*(\w+)\s*>\s*\(\s*\)");

    /// <summary>
    /// <paramref name="right"/> with each <c>FindAncestor&lt;Behavior&gt;()</c> replaced by the ancestor's instance
    /// name; unchanged when it has no call; null when any call finds no ancestor.
    /// </summary>
    public string? Resolve(string right, InstanceSave instance, StateSave stateSave)
    {
        if (!FindAncestorCall.IsMatch(right))
        {
            return right;
        }

        bool allFound = true;
        string result = FindAncestorCall.Replace(right, match =>
        {
            if (match.Groups[1].Success)
            {
                return match.Value;
            }

            string? name = FindAncestorWithBehavior(instance, stateSave, match.Groups[2].Value);
            if (name == null)
            {
                allFound = false;
                return match.Value;
            }
            return name;
        });
        return allFound ? result : null;
    }

    /// <summary>The instance named <paramref name="name"/> in <paramref name="container"/> or one of its base elements.</summary>
    public InstanceSave? FindInstanceInElementOrBase(ElementSave container, string name) =>
        ObjectFinder.Self.GetBaseElements(container)
            .Prepend(container)
            .SelectMany(element => element.Instances)
            .FirstOrDefault(item => item.Name == name);

    private string? FindAncestorWithBehavior(InstanceSave instance, StateSave stateSave, string behaviorName)
    {
        ElementSave? container = instance.ParentContainer ?? stateSave.ParentContainer;
        if (container == null)
        {
            return null;
        }

        RecursiveVariableFinder finder = new RecursiveVariableFinder(stateSave);
        InstanceSave current = instance;
        HashSet<string> visited = new HashSet<string>();
        while (visited.Add(current.Name))
        {
            string? parent = finder.GetValue(current.Name + ".Parent") as string;
            if (string.IsNullOrEmpty(parent))
            {
                return null;
            }

            // "TabControl1.TabStripInstance": the first segment is the instance in this element.
            int dotIndex = parent.IndexOf('.');
            string ownerName = dotIndex < 0 ? parent : parent.Substring(0, dotIndex);
            InstanceSave? owner = FindInstanceInElementOrBase(container, ownerName);
            if (owner == null)
            {
                return null;
            }

            ElementSave? ownerElement = ObjectFinder.Self.GetElementSave(owner.BaseType);
            if (ownerElement != null &&
                FormsBehaviorChain.GetBehaviors(ownerElement).Any(behavior => behavior.Name == behaviorName))
            {
                return owner.Name;
            }

            current = owner;
        }

        return null;
    }
}
