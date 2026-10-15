using Avalonia.Controls;
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

        grid.HoverDetailAction("X");
        ToolTip.GetTip(grid.Row("X").Displayer!).ShouldBe("Unlink (keeps the value)");

        grid.ClickDetailAction("X");

        ElementSave unlinked = grid.SelectedState.SelectedElement!;
        ReferenceLines(unlinked).ShouldBe(new[] { "//X = Leader.X" });
        grid.Member("X").IsReadOnly.ShouldBeFalse();
        VariableGridHarness.StoredValue(unlinked, "Follower.X").ShouldBe(25f);

        grid.Undo();

        ReferenceLines(grid.SelectedState.SelectedElement!).ShouldBe(new[] { "X = Leader.X" });
        grid.Member("X").IsReadOnly.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void ColorSetByAReference_ShowsOneSourceLink_AndUnlinkCommentsOutTheStoredLine()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave leader = grid.Project.AddInstance(button, "Leader", "Text");
        InstanceSave follower = grid.Project.AddInstance(button, "Follower", "Text");
        grid.Select(follower);
        grid.TypeLinesAndApply("VariableReferences", "Color = Leader.Color");

        grid.Member("Color").DetailText.ShouldBe("Set by Leader.Color");
        grid.Member("Color").DetailLink.ShouldNotBeNull();
        grid.Member("Color").DetailAction.ShouldNotBeNull();

        grid.ClickDetailLink("Color");
        grid.SelectedState.SelectedInstance.ShouldBe(leader);

        grid.Select(follower);
        grid.ClickDetailAction("Color");

        ReferenceLines(grid.SelectedState.SelectedElement!).ShouldBe(new[] { "//Color = Leader.Color" });
        grid.Member("Color").IsReadOnly.ShouldBeFalse();

        grid.Undo();

        ReferenceLines(grid.SelectedState.SelectedElement!).ShouldBe(new[] { "Color = Leader.Color" });
    }

    private static List<string> ReferenceLines(ElementSave element) =>
        ((VariableListSave<string>)element.DefaultState.VariableLists.Single(list => list.Name == "Follower.VariableReferences")).Value;
}
