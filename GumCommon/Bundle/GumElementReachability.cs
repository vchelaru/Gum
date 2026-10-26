using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Forms;
using GumRuntime;

namespace Gum.Bundle;

/// <summary>Which elements of a project are reachable from its roots, and which are not.</summary>
public class GumElementReachabilityResult
{
    /// <summary>Names of the elements to keep, Standards included.</summary>
    public IReadOnlyCollection<string> KeptElementNames { get; }

    /// <summary>Names of the Screens and Components no root reaches, sorted.</summary>
    public IReadOnlyList<string> TrimmedElementNames { get; }

    /// <summary>Roots that name no element of the project.</summary>
    public IReadOnlyList<string> UnknownRoots { get; }

    /// <summary>Initializes a new <see cref="GumElementReachabilityResult"/>.</summary>
    public GumElementReachabilityResult(IReadOnlyCollection<string> keptElementNames,
        IReadOnlyList<string> trimmedElementNames, IReadOnlyList<string> unknownRoots)
    {
        KeptElementNames = keptElementNames;
        TrimmedElementNames = trimmedElementNames;
        UnknownRoots = unknownRoots;
    }
}

/// <summary>
/// Finds the elements a project's roots reach, so <c>gumcli pack --shake</c> can leave the rest out.
/// Every Screen is a root, and so is every name in the extra roots (elements created only from code).
/// Standards are always kept. An element reaches its base type, its instances' types, the elements its
/// <c>VariableReferences</c> rows read, and the components Forms builds on its own for a control it
/// holds (a Tooltip for any control, a ListBoxItem for a ListBox, a submenu popup for a MenuItem).
/// </summary>
public class GumElementReachability
{
    // The behaviors whose components Forms creates by itself when a control with the key behavior exists.
    private static readonly Dictionary<string, string[]> CompanionBehaviors = new Dictionary<string, string[]>
    {
        [StandardFormsBehaviorNames.ListBoxBehaviorName] = new[] { StandardFormsBehaviorNames.ListBoxItemBehaviorName },
        [StandardFormsBehaviorNames.ComboBoxBehaviorName] = new[]
        {
            StandardFormsBehaviorNames.ListBoxBehaviorName, StandardFormsBehaviorNames.ListBoxItemBehaviorName,
        },
        [StandardFormsBehaviorNames.MenuBehaviorName] = new[]
        {
            StandardFormsBehaviorNames.MenuItemBehaviorName, StandardFormsBehaviorNames.MenuItemSubmenuPopupBehaviorName,
        },
        [StandardFormsBehaviorNames.MenuItemBehaviorName] = new[]
        {
            StandardFormsBehaviorNames.MenuItemBehaviorName, StandardFormsBehaviorNames.MenuItemSubmenuPopupBehaviorName,
        },
    };

    /// <summary>
    /// Walks the project from its Screens plus <paramref name="extraRoots"/>.
    /// </summary>
    /// <param name="project">The loaded project.</param>
    /// <param name="extraRoots">Names of elements to keep besides the Screens, such as those created only from code.</param>
    public GumElementReachabilityResult Compute(GumProjectSave project, IEnumerable<string> extraRoots)
    {
        if (project == null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        Dictionary<string, ElementSave> elementsByName = new Dictionary<string, ElementSave>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, ElementSave> elementsByQualifiedName = new Dictionary<string, ElementSave>(StringComparer.OrdinalIgnoreCase);
        foreach (ElementSave element in project.AllElements)
        {
            if (string.IsNullOrEmpty(element.Name) || elementsByName.ContainsKey(element.Name))
            {
                continue;
            }
            elementsByName[element.Name] = element;
            elementsByQualifiedName[ElementReference.GetQualifiedName(element, element.Name)] = element;
        }

        HashSet<string> kept = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Stack<ElementSave> pending = new Stack<ElementSave>();
        void Visit(ElementSave? element)
        {
            if (element != null && kept.Add(element.Name))
            {
                pending.Push(element);
            }
        }
        void VisitName(string? name)
        {
            if (!string.IsNullOrEmpty(name) && elementsByName.TryGetValue(name!, out ElementSave? element))
            {
                Visit(element);
            }
        }

        foreach (StandardElementSave standard in project.StandardElements)
        {
            Visit(standard);
        }
        foreach (ScreenSave screen in project.Screens)
        {
            Visit(screen);
        }
        List<string> unknownRoots = new List<string>();
        foreach (string root in extraRoots)
        {
            if (elementsByName.TryGetValue(root, out ElementSave? element))
            {
                Visit(element);
            }
            else
            {
                unknownRoots.Add(root);
            }
        }

        while (pending.Count > 0)
        {
            ElementSave element = pending.Pop();
            VisitName(element.BaseType);
            foreach (InstanceSave instance in element.Instances)
            {
                VisitName(instance.BaseType);
            }
            foreach (string qualifiedName in ReferenceDependencies.GetElementsRead(element))
            {
                if (elementsByQualifiedName.TryGetValue(qualifiedName, out ElementSave? read))
                {
                    Visit(read);
                }
            }
            if (element is ComponentSave component && component.Behaviors.Count > 0)
            {
                Visit(DefaultImplementationLookup.Find(project, StandardFormsBehaviorNames.TooltipBehaviorName));
                foreach (ElementBehaviorReference behavior in component.Behaviors)
                {
                    if (behavior.BehaviorName != null
                        && CompanionBehaviors.TryGetValue(behavior.BehaviorName, out string[]? companions))
                    {
                        foreach (string companion in companions)
                        {
                            Visit(DefaultImplementationLookup.Find(project, companion));
                        }
                    }
                }
            }
        }

        List<string> trimmed = project.Screens.Cast<ElementSave>().Concat(project.Components)
            .Select(element => element.Name)
            .Where(name => !string.IsNullOrEmpty(name) && !kept.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        return new GumElementReachabilityResult(kept, trimmed, unknownRoots);
    }
}
