using Avalonia.Headless.XUnit;
using Gum.DataTypes;
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
}
