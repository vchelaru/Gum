using System.Diagnostics;
using MonoGameGum.Tests;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Dragging a value reports an intermediate commit per tick and one full commit on release (#6888). Only the
/// full commit runs the variable reference cascade; the result after the release must be the one a single
/// typed edit of the final value gives.
/// </summary>
public class DragCommitCascadeTests : BaseTestClass
{
    [Fact]
    public void Tick_ShouldLeaveElementsReadingTheValueUntouched_UntilTheFullCommit()
    {
        using DragWorld world = new DragWorld();
        world.BuildScale(readers: 3, bystanders: 0);
        world.WidthOf(world.Component("C0"), "Default", "Box").ShouldBe(51);

        world.Tick("S0", "Default", 80);
        world.Tick("S0", "Default", 90);

        world.WidthOf(world.Comp, "Default", "S0").ShouldBe(90);
        world.WidthOf(world.Component("C0"), "Default", "Box").ShouldBe(51);

        world.Commit("S0", "Default", 90);

        world.WidthOf(world.Component("C0"), "Default", "Box").ShouldBe(91);
        world.WidthOf(world.Component("C2"), "Default", "Box").ShouldBe(91);
    }

    [Fact]
    public void Tick_ShouldUpdateSameElementFollowers_ButNotOtherElements_UntilTheFullCommit()
    {
        using DragWorld world = new DragWorld();
        world.BuildSameElement();
        world.WidthOf(world.Comp, "Default", "F0").ShouldBe(51);
        world.WidthOf(world.Component("Other"), "Default", "O0").ShouldBe(50);
        int savesBefore = world.SaveCount;
        int historyBefore = world.HistoryCount;

        world.Tick("S0", "Default", 80);
        world.Tick("S0", "Default", 90);

        world.WidthOf(world.Comp, "Default", "F0").ShouldBe(91);
        world.WidthOf(world.Component("Other"), "Default", "O0").ShouldBe(50);
        world.SaveCount.ShouldBe(savesBefore);
        world.HistoryCount.ShouldBe(historyBefore);

        world.Commit("S0", "Default", 90);

        world.WidthOf(world.Comp, "Default", "F0").ShouldBe(91);
        world.WidthOf(world.Component("Other"), "Default", "O0").ShouldBe(90);
        world.HistoryCount.ShouldBe(historyBefore + 1);
        world.MemoryEqualsDisk().ShouldBeTrue();
    }

    [Fact]
    public void Tick_ShouldWriteToolOnlyBehaviorReferences_WithoutSavingOrRecordingUndo()
    {
        using DragWorld world = new DragWorld();
        world.BuildSameElement();
        int savesBefore = world.SaveCount;
        int historyBefore = world.HistoryCount;

        world.TickSpacing(12);

        // What the editor reads for a StackPanel (StackSpacing = Spacing) while the value is dragged.
        Convert.ToSingle(world.StackSpacing).ShouldBe(12f);
        world.SaveCount.ShouldBe(savesBefore);
        world.HistoryCount.ShouldBe(historyBefore);

        world.CommitSpacing(12);

        Convert.ToSingle(world.StackSpacing).ShouldBe(12f);
        world.HistoryCount.ShouldBe(historyBefore + 1);
    }

    [Fact]
    public void FullCommit_ShouldUpdateReadersAndSaveThem_WithoutAnyTicksBefore()
    {
        using DragWorld world = new DragWorld();
        world.BuildScale(readers: 3, bystanders: 0);

        world.Commit("S0", "Default", 70);

        world.WidthOf(world.Component("C1"), "Default", "Box").ShouldBe(71);
        world.MemoryEqualsDisk().ShouldBeTrue();
        world.HistoryCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(1000, 0)]
    [InlineData(0, 1000)]
    public void Tick_ShouldCostTheSameWhateverTheProjectSize(int readers, int bystanders)
    {
        using DragWorld world = new DragWorld();
        world.BuildScale(readers, bystanders);
        world.Tick("S0", "Default", 59);

        List<double> milliseconds = new List<double>();
        for (int i = 0; i < 7; i++)
        {
            Stopwatch watch = Stopwatch.StartNew();
            world.Tick("S0", "Default", 60 + i);
            watch.Stop();
            milliseconds.Add(watch.Elapsed.TotalMilliseconds);
        }
        milliseconds.Sort();

        // A wide margin: with a thousand elements reading the value a tick re-evaluated them all.
        milliseconds[3].ShouldBeLessThan(20.0);
    }

    [Fact]
    public void Drag_ShouldGiveWhatOneTypedEditGives_OverRandomProjects()
    {
        int cases = SweepScale.Cases(300);
        Random random = new Random(6888);
        List<string> failures = new List<string>();
        for (int caseIndex = 0; caseIndex < cases; caseIndex++)
        {
            int buildSeed = random.Next();
            bool autoSave = random.Next(2) == 0;
            bool useAlt = random.Next(3) == 0;
            int tickCount = random.Next(1, 6);
            List<float> ticks = new List<float>();
            for (int i = 0; i < tickCount; i++)
            {
                ticks.Add(random.Next(1, 200));
            }
            float final = random.Next(1, 200);
            ticks.Add(final);
            int draggedPick = random.Next();
            bool dragInAlt = random.Next(2) == 0;

            string description;
            SortedDictionary<string, string> dragged;
            int draggedHistory;
            bool draggedMatchesDisk;
            using (DragWorld world = new DragWorld())
            {
                world.BuildRandom(new Random(buildSeed), autoSave, useAlt);
                description = world.Description;
                (string instance, string state) = Target(world, draggedPick, dragInAlt);
                foreach (float tick in ticks.Take(ticks.Count - 1))
                {
                    world.Tick(instance, state, tick);
                }
                world.Commit(instance, state, final);
                dragged = world.Snap();
                draggedHistory = world.HistoryCount;
                draggedMatchesDisk = world.MemoryEqualsDisk();
                description += $" | drag {state}:{instance} {string.Join(",", ticks)}";
            }

            SortedDictionary<string, string> typed;
            int typedHistory;
            using (DragWorld world = new DragWorld())
            {
                world.BuildRandom(new Random(buildSeed), autoSave, useAlt);
                (string instance, string state) = Target(world, draggedPick, dragInAlt);
                world.Commit(instance, state, final);
                typed = world.Snap();
                typedHistory = world.HistoryCount;
            }

            List<string> diffs = dragged.Keys.Union(typed.Keys).OrderBy(key => key)
                .Where(key => dragged.GetValueOrDefault(key) != typed.GetValueOrDefault(key))
                .Select(key => $"{key}: drag={dragged.GetValueOrDefault(key) ?? "-"} typed={typed.GetValueOrDefault(key) ?? "-"}")
                .ToList();
            if (draggedHistory != typedHistory)
            {
                diffs.Add($"history drag={draggedHistory} typed={typedHistory}");
            }
            if (!draggedMatchesDisk)
            {
                diffs.Add("memory differs from disk after the drag");
            }
            if (diffs.Count > 0)
            {
                failures.Add($"case {caseIndex}: {description}\n  {string.Join("\n  ", diffs)}");
            }
        }

        failures.Count.ShouldBe(0, string.Join("\n", failures.Take(3)));
    }

    private static (string Instance, string State) Target(DragWorld world, int pick, bool inAlt)
    {
        if (world.HasAlt && inAlt)
        {
            return ("S0", "Alt");
        }
        return (world.CompInstances[pick % world.CompInstances.Count], "Default");
    }
}
