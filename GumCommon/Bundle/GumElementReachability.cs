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
    /// <summary>The key in <see cref="FormsCreatedBehaviors"/> that applies to a component with any behavior.</summary>
    public const string AnyFormsBehavior = "*";

    /// <summary>
    /// For a component with the key behavior, the behaviors whose components the Forms runtime builds by
    /// itself (a parameterless <c>new Control()</c> takes its visual from DefaultFormsTemplates, which is the
    /// behavior's DefaultImplementation in generated code). <c>GumElementReachabilityTests</c> checks this
    /// against every parameterless construction in <c>MonoGameGum/Forms/Controls</c>.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> FormsCreatedBehaviors { get; } =
        new Dictionary<string, IReadOnlyList<string>>
        {
            // FrameworkElement.ToolTip and ToolTipService.SetToolTip (the Tooltip is built in ToolTipService). A project with no Forms component kept that only calls SetToolTip on plain visuals needs "Tooltip" in the extra roots.
            [AnyFormsBehavior] = new[] { StandardFormsBehaviorNames.TooltipBehaviorName },
            // ItemsControl.CreateNewItemFrameworkElement falls back to new Label(); CreateNewVisual uses the
            // ListBoxItem template
            [StandardFormsBehaviorNames.ItemsControlBehaviorName] = new[]
            {
                StandardFormsBehaviorNames.LabelBehaviorName, StandardFormsBehaviorNames.ListBoxItemBehaviorName,
            },
            [StandardFormsBehaviorNames.ListBoxBehaviorName] = new[] { StandardFormsBehaviorNames.ListBoxItemBehaviorName },
            // FormsUtilities' drop-down template, then the ListBox's items
            [StandardFormsBehaviorNames.ComboBoxBehaviorName] = new[]
            {
                StandardFormsBehaviorNames.ListBoxBehaviorName, StandardFormsBehaviorNames.ListBoxItemBehaviorName,
            },
            // Menu.CreateNewItemFrameworkElement, then each MenuItem's popup
            [StandardFormsBehaviorNames.MenuBehaviorName] = new[]
            {
                StandardFormsBehaviorNames.MenuItemBehaviorName, StandardFormsBehaviorNames.MenuItemSubmenuPopupBehaviorName,
                StandardFormsBehaviorNames.ScrollViewerBehaviorName,
            },
            // The submenu popup: the MenuItemSubmenuPopup component, else MenuItem.CreateProjectScrollViewerVisual's
            // ScrollViewer component, else new ScrollViewer(); its rows are new MenuItem()
            [StandardFormsBehaviorNames.MenuItemBehaviorName] = new[]
            {
                StandardFormsBehaviorNames.MenuItemBehaviorName, StandardFormsBehaviorNames.MenuItemSubmenuPopupBehaviorName,
                StandardFormsBehaviorNames.ScrollViewerBehaviorName,
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
                IEnumerable<string> keys = component.Behaviors
                    .Where(behavior => !string.IsNullOrEmpty(behavior.BehaviorName))
                    .Select(behavior => behavior.BehaviorName!)
                    .Append(AnyFormsBehavior);
                foreach (string key in keys)
                {
                    if (FormsCreatedBehaviors.TryGetValue(key, out IReadOnlyList<string>? created))
                    {
                        foreach (string createdBehavior in created)
                        {
                            Visit(DefaultImplementationLookup.Find(project, createdBehavior));
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
