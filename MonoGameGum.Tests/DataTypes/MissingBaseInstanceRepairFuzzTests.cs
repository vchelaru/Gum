using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

/// <summary>
/// Fixed-seed random inheritance graphs (#6597): after the repair every element holds each inherited
/// instance exactly once, in the order a correct file has them, with the instances it already held in
/// their original relative order; a second repair changes nothing.
/// </summary>
public class MissingBaseInstanceRepairFuzzTests : BaseTestClass
{
    [Fact]
    public void RepairMissingDefinedByBaseInstances_RandomInheritanceGraphs_AddsEachInheritedInstanceOnceInBaseOrder()
    {
        Random random = new Random(6597);
        int cases = SweepScale.Cases(400);
        int checkedOrder = 0;

        for (int iteration = 0; iteration < cases; iteration++)
        {
            int count = random.Next(1, 7);
            List<string> names = Enumerable.Range(0, count).Select(index => "E" + index).ToList();
            Dictionary<string, string?> baseOf = CreateBaseTypes(random, names);
            bool isCyclic = names.Any(name => ReachesCycle(name, baseOf));
            Dictionary<string, List<string>> own = names.ToDictionary(
                name => name, name => Enumerable.Range(0, random.Next(0, 4)).Select(index => name + "_i" + index).ToList());
            Dictionary<string, List<string>> correct = new Dictionary<string, List<string>>();
            if (!isCyclic)
            {
                foreach (string name in names)
                {
                    CorrectOrder(name, baseOf, own, correct);
                }
            }

            bool shuffleHeld = random.Next(5) == 0;
            GumProjectSave project = new GumProjectSave();
            Dictionary<string, ComponentSave> components = new Dictionary<string, ComponentSave>();
            Dictionary<string, List<string>> heldBefore = new Dictionary<string, List<string>>();
            foreach (string name in names)
            {
                List<string> held = isCyclic
                    ? new List<string>(own[name])
                    : correct[name].Where(item => own[name].Contains(item) || random.Next(3) != 0).ToList();
                if (shuffleHeld)
                {
                    held = held.Where(item => !own[name].Contains(item)).OrderBy(_ => random.Next())
                        .Concat(held.Where(item => own[name].Contains(item))).ToList();
                }

                ComponentSave component = new ComponentSave { Name = name, BaseType = baseOf[name] };
                component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
                foreach (string instanceName in held)
                {
                    component.Instances.Add(new InstanceSave
                    {
                        Name = instanceName, BaseType = "Container", ParentContainer = component,
                        DefinedByBase = !own[name].Contains(instanceName),
                    });
                }
                heldBefore[name] = held;
                components[name] = component;
                project.Components.Add(component);
            }

            // The order the elements are processed in changes which base is still incomplete.
            List<ComponentSave> processingOrder = project.Components.OrderBy(_ => random.Next()).ToList();
            project.Components.Clear();
            project.Components.AddRange(processingOrder);

            project.RepairMissingDefinedByBaseInstances();

            Dictionary<string, List<string>> after = names.ToDictionary(
                name => name, name => components[name].Instances.Select(item => item.Name).ToList());
            foreach (string name in names)
            {
                string context = $"iteration {iteration} element {name} cyclic={isCyclic} before=[{string.Join(",", heldBefore[name])}] " +
                    $"after=[{string.Join(",", after[name])}] bases={string.Join(";", names.Select(item => item + "<-" + baseOf[item]))}";
                after[name].Distinct().Count().ShouldBe(after[name].Count, context);
                after[name].Where(heldBefore[name].Contains).ToList().ShouldBe(heldBefore[name], context);
                components[name].Instances.ShouldAllBe(item => item.ParentContainer == components[name], context);
                if (isCyclic)
                {
                    foreach (string reachable in ReachableBases(name, baseOf))
                    {
                        foreach (string inherited in own[reachable])
                        {
                            after[name].ShouldContain(inherited, context);
                        }
                    }
                }
                else
                {
                    after[name].OrderBy(item => item).ToList().ShouldBe(correct[name].OrderBy(item => item).ToList(), context);
                    if (!shuffleHeld)
                    {
                        after[name].ShouldBe(correct[name], context);
                        checkedOrder++;
                    }
                }
            }

            project.RepairMissingDefinedByBaseInstances().ShouldBeFalse($"iteration {iteration} second call");
            foreach (string name in names)
            {
                components[name].Instances.Select(item => item.Name).ToList().ShouldBe(after[name], $"iteration {iteration} second call {name}");
            }
        }

        checkedOrder.ShouldBeGreaterThan(cases / 4);
    }

    // Mostly a tree over earlier elements; sometimes an unknown base, Container, no base, or a back edge that makes a cycle.
    private static Dictionary<string, string?> CreateBaseTypes(Random random, List<string> names)
    {
        Dictionary<string, string?> baseOf = new Dictionary<string, string?>();
        for (int index = 0; index < names.Count; index++)
        {
            int roll = random.Next(10);
            if (roll < 2 || index == 0)
            {
                baseOf[names[index]] = roll == 1 ? "Nope" : (random.Next(2) == 0 ? "Container" : null);
            }
            else if (roll == 9)
            {
                baseOf[names[index]] = names[random.Next(names.Count)];
            }
            else
            {
                baseOf[names[index]] = names[random.Next(index)];
            }
        }
        return baseOf;
    }

    private static bool ReachesCycle(string name, Dictionary<string, string?> baseOf)
    {
        HashSet<string> seen = new HashSet<string>();
        string? current = name;
        while (current != null && baseOf.ContainsKey(current))
        {
            if (!seen.Add(current))
            {
                return true;
            }
            current = baseOf[current];
        }
        return false;
    }

    private static IEnumerable<string> ReachableBases(string name, Dictionary<string, string?> baseOf)
    {
        HashSet<string> seen = new HashSet<string> { name };
        string? current = baseOf[name];
        while (current != null && baseOf.ContainsKey(current) && seen.Add(current))
        {
            yield return current;
            current = baseOf[current];
        }
    }

    private static List<string> CorrectOrder(
        string name, Dictionary<string, string?> baseOf, Dictionary<string, List<string>> own, Dictionary<string, List<string>> cache)
    {
        if (cache.TryGetValue(name, out List<string>? known))
        {
            return known;
        }
        List<string> list = new List<string>();
        string? baseName = baseOf[name];
        if (baseName != null && baseOf.ContainsKey(baseName))
        {
            list.AddRange(CorrectOrder(baseName, baseOf, own, cache));
        }
        list.AddRange(own[name]);
        cache[name] = list;
        return list;
    }
}
