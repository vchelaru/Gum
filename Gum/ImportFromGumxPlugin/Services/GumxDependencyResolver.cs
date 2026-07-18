using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.ProjectServices;
using System.Collections.Generic;
using System.Linq;

namespace ImportFromGumxPlugin.Services;

public class GumxDependencyResolver : IGumxDependencyResolver
{
    private readonly IStandardComparer _standardComparer;

    public GumxDependencyResolver() : this(new StandardComparer()) { }

    public GumxDependencyResolver(IStandardComparer standardComparer)
    {
        _standardComparer = standardComparer;
    }

    /// <inheritdoc/>
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

        // Track which component names are directly selected so we can exclude them from transitive
        var directNames = new HashSet<string>(
            directSelected.OfType<ComponentSave>().Select(c => c.Name));

        // Use ObjectFinder to discover all transitive references (inheritance + composition)
        var objectFinder = new ObjectFinder { GumProjectSave = source };

        var allTransitiveComponents = new Dictionary<string, ComponentSave>();
        var referencedStandardNames = new HashSet<string>();
        var behaviorNames = new HashSet<string>();

        foreach (var element in directSelected)
        {
            CollectBehaviorNames(element, behaviorNames);

            var referenced = objectFinder.GetElementsReferencedByThis(element);
            foreach (var refElement in referenced)
            {
                if (refElement == null) continue;

                if (refElement is ComponentSave comp)
                {
                    allTransitiveComponents.TryAdd(comp.Name, comp);
                }
                else if (refElement is StandardElementSave)
                {
                    referencedStandardNames.Add(refElement.Name);
                }
            }
        }

        // Collect behaviors from transitive components too
        foreach (var comp in allTransitiveComponents.Values)
        {
            CollectBehaviorNames(comp, behaviorNames);
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
        var sourceStandardsByName = source.StandardElements
            .ToDictionary(s => s.Name, s => s);
        var destinationStandardsByName = destination.StandardElements
            .ToDictionary(s => s.Name, s => s);
        foreach (var standardName in referencedStandardNames)
        {
            if (!sourceStandardsByName.TryGetValue(standardName, out var sourceStandard)) continue;

            if (destinationStandardsByName.TryGetValue(standardName, out var destStandard))
            {
                StandardComparisonResult comparison = _standardComparer.Compare(sourceStandard, destStandard);
                if (comparison.HasDifferences)
                {
                    result.DifferingStandards.Add(sourceStandard);
                    result.DifferingStandardDiffs[sourceStandard] = comparison;
                }
            }
            else
            {
                // Not in destination at all — include it with a synthesized "differs" result
                // so the dialog can still surface the row (with no per-variable detail).
                result.DifferingStandards.Add(sourceStandard);
                result.DifferingStandardDiffs[sourceStandard] = new StandardComparisonResult
                {
                    HasDifferences = true
                };
            }
        }

        return result;
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

            // Visit base type first (inheritance edge — base must come before derived)
            if (!string.IsNullOrEmpty(component.BaseType)
                && componentSet.Contains(component.BaseType)
                && sourceComponentsByName.TryGetValue(component.BaseType, out var baseDep))
            {
                Visit(baseDep);
            }

            // Visit composition dependencies (so leaves come before parents)
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

}
