using System.Collections.Specialized;
using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// Editing VariableReferences on many selected instances rebuilt the whole Variables grid twice per
/// instance (about 0.7 s each on a 19-Rectangle screen, ~27 s in all). The batch rebuilds it once.
/// </summary>
public class MultiSelectReferenceEditScenarioTests
{
    private static int CountGridRebuildsWhenEditingReferencesOn(int instanceCount)
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ScreenSave screen = grid.Project.AddScreen("Main");
        List<InstanceSave> instances = new List<InstanceSave>();
        for (int i = 0; i < instanceCount; i++)
        {
            instances.Add(grid.Project.AddInstance(screen, "Rect" + i, "Rectangle"));
        }
        grid.Select(instances.ToArray());

        int notifications = 0;
        NotifyCollectionChangedEventHandler handler = (_, _) => notifications++;
        grid.Grid.Categories.CollectionChanged += handler;
        try
        {
            grid.TypeLinesAndApply("VariableReferences", "X = 100 * Sin(@Index * 21)");
        }
        finally
        {
            grid.Grid.Categories.CollectionChanged -= handler;
        }

        screen.DefaultState.GetVariableListSave("Rect0.VariableReferences").ShouldNotBeNull();
        screen.DefaultState.GetVariableListSave("Rect" + (instanceCount - 1) + ".VariableReferences").ShouldNotBeNull();
        return notifications;
    }

    [AvaloniaFact]
    public void EditingReferences_RebuildsTheGridOnceWhateverTheInstanceCount()
    {
        int forThree = CountGridRebuildsWhenEditingReferencesOn(3);
        int forEight = CountGridRebuildsWhenEditingReferencesOn(8);

        forEight.ShouldBe(forThree);
    }
}
