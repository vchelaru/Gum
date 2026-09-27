using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.ProjectServices;
using System;
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
        // Standard name -> the names of the elements that use it.
        var standardUsers = new Dictionary<string, SortedSet<string>>();
        var behaviorNames = new HashSet<string>();

        // A component a variable reference reads from is a dependency too, and brings its own, so
        // each newly found component is walked in turn.
        var pending = new Queue<ElementSave>(directSelected);
        var walked = new HashSet<ElementSave>();
        while (pending.Count > 0)
        {
            var element = pending.Dequeue();
            if (!walked.Add(element)) continue;

            CollectBehaviorNames(element, behaviorNames);

            var referenced = objectFinder.GetElementsReferencedByThis(element);
            foreach (var refElement in referenced)
            {
                if (refElement == null) continue;

                if (refElement is ComponentSave comp)
                {
                    allTransitiveComponents.TryAdd(comp.Name, comp);
                    pending.Enqueue(comp);
                }
                else if (refElement is StandardElementSave)
                {
                    if (!standardUsers.TryGetValue(refElement.Name, out var users))
                    {
                        users = new SortedSet<string>(StringComparer.Ordinal);
                        standardUsers[refElement.Name] = users;
                    }
                    users.Add(element.Name);
                }
            }

            foreach (var name in GetComponentsReadByVariableReferences(element))
            {
                if (sourceComponentsByName.TryGetValue(name, out var comp))
                {
                    allTransitiveComponents.TryAdd(comp.Name, comp);
                    pending.Enqueue(comp);
                }
            }
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
        foreach (var (standardName, users) in standardUsers)
        {
            if (!sourceStandardsByName.TryGetValue(standardName, out var sourceStandard)) continue;

            // Not in destination at all — include it with a synthesized "differs" result
            // so the dialog can still surface the row (with no per-variable detail).
            StandardComparisonResult comparison =
                destinationStandardsByName.TryGetValue(standardName, out var destStandard)
                    ? _standardComparer.Compare(sourceStandard, destStandard)
                    : new StandardComparisonResult { HasDifferences = true };
            if (comparison.HasDifferences)
            {
                result.DifferingStandards.Add(sourceStandard);
                result.DifferingStandardDiffs[sourceStandard] = comparison;
                result.DifferingStandardUsers[sourceStandard] = users.ToList();
            }
        }

        return result;
    }

    /// <summary>
    /// The components named on the right of the element's <c>VariableReferences</c> lines
    /// (<c>Color = Components/Styles.Primary.FillColor</c> reads from <c>Styles</c>), on the
    /// element itself and on its instances.
    /// </summary>
    private static IEnumerable<string> GetComponentsReadByVariableReferences(ElementSave element)
    {
        const string componentsPrefix = "Components/";
        foreach (var state in element.AllStates)
        {
            foreach (var variableList in state.VariableLists)
            {
                if (variableList.GetRootName() != "VariableReferences") continue;

                foreach (var item in variableList.ValueAsIList)
                {
                    if (item is not string line) continue;

                    int equalsIndex = line.IndexOf('=');
                    if (equalsIndex < 0) continue;

                    string right = line.Substring(equalsIndex + 1).Trim();
                    if (!right.StartsWith(componentsPrefix, System.StringComparison.Ordinal)) continue;

                    // Element names hold no dots, so the name ends where the variable begins.
                    int dotIndex = right.IndexOf('.', componentsPrefix.Length);
                    if (dotIndex > componentsPrefix.Length)
                    {
                        yield return right.Substring(componentsPrefix.Length, dotIndex - componentsPrefix.Length);
                    }
                }
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

            // Components its variable references read from, so their values exist when it imports
            foreach (var name in GetComponentsReadByVariableReferences(component))
            {
                if (componentSet.Contains(name) && sourceComponentsByName.TryGetValue(name, out var referencedDep))
                {
                    Visit(referencedDep);
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
