using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace ImportFromGumxPlugin.Services;

/// <summary>
/// The result of a transitive dependency computation.
/// </summary>
public class DependencySet
{
    /// <summary>
    /// Components that are transitively required by the selected items but not directly selected.
    /// These are ordered so that leaves of the dependency graph come first (safe import order).
    /// </summary>
    public List<ComponentSave> TransitiveComponents { get; } = new List<ComponentSave>();

    /// <summary>
    /// Behaviors required by any selected or transitive component.
    /// </summary>
    public List<BehaviorSave> Behaviors { get; } = new List<BehaviorSave>();

    /// <summary>
    /// Standards referenced by any selected/transitive component that differ
    /// from the corresponding standard in the destination project.
    /// </summary>
    public List<StandardElementSave> DifferingStandards { get; } = new List<StandardElementSave>();
}

public class GumxDependencyResolver
{
    /// <summary>
    /// Computes the full transitive dependency closure for the given directly-selected elements.
    /// Items already present in the destination project are excluded from the transitive component list.
    /// Standards are shown regardless of whether they exist in the destination — only differing ones appear.
    /// </summary>
    public DependencySet ComputeTransitive(
        IList<ElementSave> directSelected,
        GumProjectSave source,
        GumProjectSave destination)
    {
        var result = new DependencySet();

        // Build lookup sets for source items
        var sourceComponentsByName = source.Components
            .ToDictionary(c => c.Name, c => c);
        var sourceBehaviorsByName = source.Behaviors
            .ToDictionary(b => b.Name, b => b);
        var sourceStandardsByName = source.StandardElements
            .ToDictionary(s => s.Name, s => s);

        // Track which component names are directly selected so we can exclude them from transitive
        var directNames = new HashSet<string>(
            directSelected.OfType<ComponentSave>().Select(c => c.Name));

        // Also include screens for dependency analysis
        var allDirectElements = directSelected.ToList();

        // BFS to find the full transitive closure
        var visited = new HashSet<string>(directNames);
        var queue = new Queue<ElementSave>(allDirectElements);
        var transitiveOrder = new List<ComponentSave>(); // added in BFS order (deepest first via post-order)

        // We'll use a topological sort approach: process dependencies before the element itself
        // Build the full closure first, then topologically sort
        var allTransitiveComponents = new Dictionary<string, ComponentSave>();
        var behaviorNames = new HashSet<string>();
        var referencedStandardNames = new HashSet<string>();

        // Collect direct behaviors
        foreach (var element in allDirectElements)
        {
            CollectBehaviorNames(element, behaviorNames);
            CollectStandardNames(element, sourceStandardsByName, referencedStandardNames);
        }

        // BFS over component dependencies.
        // The queue starts with directly selected components. Screen-level deps are
        // enqueued first so they also appear in allTransitiveComponents.
        var componentQueue = new Queue<ComponentSave>(
            allDirectElements.OfType<ComponentSave>());

        // Screen deps are transitive — add them to allTransitiveComponents too
        foreach (var screen in allDirectElements.OfType<ScreenSave>())
        {
            EnqueueComponentDeps(screen, sourceComponentsByName, visited, componentQueue, allTransitiveComponents);
        }

        while (componentQueue.Count > 0)
        {
            var current = componentQueue.Dequeue();
            foreach (var instance in current.Instances)
            {
                if (string.IsNullOrEmpty(instance.BaseType)) continue;

                // Check if it's a source component
                if (sourceComponentsByName.TryGetValue(instance.BaseType, out var dep))
                {
                    if (visited.Add(dep.Name))
                    {
                        allTransitiveComponents[dep.Name] = dep;
                        componentQueue.Enqueue(dep);
                    }
                }

                // Check standard references
                if (sourceStandardsByName.ContainsKey(instance.BaseType))
                {
                    referencedStandardNames.Add(instance.BaseType);
                }
            }

            CollectBehaviorNames(current, behaviorNames);
            CollectStandardNames(current, sourceStandardsByName, referencedStandardNames);
        }

        // Remove directly selected components from the transitive set
        foreach (var name in directNames)
        {
            allTransitiveComponents.Remove(name);
        }

        // Remove components already present in the destination
        var destinationComponentNames = new HashSet<string>(destination.Components.Select(c => c.Name));
        var filteredTransitive = allTransitiveComponents.Values
            .Where(c => !destinationComponentNames.Contains(c.Name))
            .ToList();

        // Topological sort (leaves first so imports don't fail on missing base types)
        var sorted = TopologicalSort(filteredTransitive, sourceComponentsByName);
        result.TransitiveComponents.AddRange(sorted);

        // Collect behaviors (excluding those already in destination)
        var destinationBehaviorNames = new HashSet<string>(destination.Behaviors.Select(b => b.Name));
        foreach (var behaviorName in behaviorNames)
        {
            if (sourceBehaviorsByName.TryGetValue(behaviorName, out var behavior)
                && !destinationBehaviorNames.Contains(behavior.Name))
            {
                result.Behaviors.Add(behavior);
            }
        }

        // Find differing standards (referenced by any selected/transitive item, differ from destination)
        var destinationStandardsByName = destination.StandardElements
            .ToDictionary(s => s.Name, s => s);
        foreach (var standardName in referencedStandardNames)
        {
            if (!sourceStandardsByName.TryGetValue(standardName, out var sourceStandard)) continue;

            if (destinationStandardsByName.TryGetValue(standardName, out var destStandard))
            {
                // Compare default states via XML
                if (StandardsDiffer(sourceStandard, destStandard))
                {
                    result.DifferingStandards.Add(sourceStandard);
                }
            }
            else
            {
                // Not in destination at all — include it
                result.DifferingStandards.Add(sourceStandard);
            }
        }

        return result;
    }

    private static void EnqueueComponentDeps(
        ElementSave element,
        Dictionary<string, ComponentSave> sourceComponentsByName,
        HashSet<string> visited,
        Queue<ComponentSave> queue,
        Dictionary<string, ComponentSave>? transitiveComponents = null)
    {
        foreach (var instance in element.Instances)
        {
            if (!string.IsNullOrEmpty(instance.BaseType)
                && sourceComponentsByName.TryGetValue(instance.BaseType, out var dep)
                && visited.Add(dep.Name))
            {
                transitiveComponents?.TryAdd(dep.Name, dep);
                queue.Enqueue(dep);
            }
        }
    }

    private static void CollectBehaviorNames(ElementSave element, HashSet<string> behaviorNames)
    {
        foreach (var behavior in element.Behaviors)
        {
            if (!string.IsNullOrEmpty(behavior.BehaviorName))
            {
                behaviorNames.Add(behavior.BehaviorName);
            }
        }
    }

    private static void CollectStandardNames(
        ElementSave element,
        Dictionary<string, StandardElementSave> sourceStandardsByName,
        HashSet<string> referencedStandardNames)
    {
        // Check the element's own BaseType
        if (!string.IsNullOrEmpty(element.BaseType)
            && sourceStandardsByName.ContainsKey(element.BaseType))
        {
            referencedStandardNames.Add(element.BaseType);
        }

        // Check instance base types
        foreach (var instance in element.Instances)
        {
            if (!string.IsNullOrEmpty(instance.BaseType)
                && sourceStandardsByName.ContainsKey(instance.BaseType))
            {
                referencedStandardNames.Add(instance.BaseType);
            }
        }
    }

    private static List<ComponentSave> TopologicalSort(
        List<ComponentSave> components,
        Dictionary<string, ComponentSave> sourceComponentsByName)
    {
        var componentSet = new HashSet<string>(components.Select(c => c.Name));
        var sorted = new List<ComponentSave>();
        var visited = new HashSet<string>();

        void Visit(ComponentSave component)
        {
            if (!visited.Add(component.Name)) return;

            // Visit dependencies first (so leaves come before parents)
            foreach (var instance in component.Instances)
            {
                if (!string.IsNullOrEmpty(instance.BaseType)
                    && componentSet.Contains(instance.BaseType)
                    && sourceComponentsByName.TryGetValue(instance.BaseType, out var dep))
                {
                    Visit(dep);
                }
            }

            sorted.Add(component);
        }

        foreach (var component in components)
        {
            Visit(component);
        }

        return sorted;
    }

    private static bool StandardsDiffer(StandardElementSave source, StandardElementSave destination)
    {
        var sourceDefault = source.DefaultState;
        var destDefault = destination.DefaultState;

        if (sourceDefault == null && destDefault == null) return false;
        if (sourceDefault == null || destDefault == null) return true;

        // Clone and sort variables before comparing, matching the pattern in AddFormsViewModel
        var sourceClone = sourceDefault.Clone();
        var destClone = destDefault.Clone();
        sourceClone.Variables.Sort((a, b) => a.Name.CompareTo(b.Name));
        destClone.Variables.Sort((a, b) => a.Name.CompareTo(b.Name));

        FileManager.XmlSerialize(sourceClone, out string sourceSerialized);
        FileManager.XmlSerialize(destClone, out string destSerialized);

        return sourceSerialized != destSerialized;
    }
}
