using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>The source named under a reference-driven row is a link that selects the source.</summary>
public class VariableReferenceLinkScenarioTests
{
    [AvaloniaFact]
    public void ClickingTheSourceUnderAReferenceRow_SelectsTheSourceInstance()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave leader = grid.Project.AddInstance(button, "Leader", "Container");
        InstanceSave follower = grid.Project.AddInstance(button, "Follower", "Container");
        grid.Select(follower);
        grid.TypeLinesAndApply("VariableReferences", "X = Leader.X");
        grid.Member("X").IsReadOnly.ShouldBeTrue();

        grid.ClickDetailLink("X");

        grid.SelectedState.SelectedInstance.ShouldBe(leader);
    }

    [AvaloniaFact]
    public void AnExpressionReference_ShowsNoLink()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Project.AddInstance(button, "Leader", "Container");
        InstanceSave follower = grid.Project.AddInstance(button, "Follower", "Container");
        grid.Select(follower);
        grid.TypeLinesAndApply("VariableReferences", "X = Leader.X + 10");

        grid.Member("X").DetailText.ShouldBe("Set by Leader.X + 10");
        grid.Member("X").DetailLink.ShouldBeNull();
    }

    [AvaloniaFact]
    public void ClickingUnlink_CommentsOutTheReference_KeepsTheValue_AndUndoRestoresIt()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave leader = grid.Project.AddInstance(button, "Leader", "Container");
        InstanceSave follower = grid.Project.AddInstance(button, "Follower", "Container");
        grid.Select(leader);
        grid.TypeAndEnter("X", "25");
        grid.Select(follower);
        grid.TypeLinesAndApply("VariableReferences", "X = Leader.X");
        grid.Member("X").DetailAction.ShouldNotBeNull();

        grid.ClickDetailAction("X");

        ElementSave unlinked = grid.SelectedState.SelectedElement!;
        ReferenceLines(unlinked).ShouldBe(new[] { "//X = Leader.X" });
        grid.Member("X").IsReadOnly.ShouldBeFalse();
        VariableGridHarness.StoredValue(unlinked, "Follower.X").ShouldBe(25f);

        grid.Undo();

        ReferenceLines(grid.SelectedState.SelectedElement!).ShouldBe(new[] { "X = Leader.X" });
        grid.Member("X").IsReadOnly.ShouldBeTrue();
    }

    private static List<string> ReferenceLines(ElementSave element) =>
        ((VariableListSave<string>)element.DefaultState.VariableLists.Single(list => list.Name == "Follower.VariableReferences")).Value;
}
