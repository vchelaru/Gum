using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// ScrollBar and ScrollViewer declare LargeChange with no default (#6255): the runtime computes it
/// from the viewport unless the Visual authors a value, so the tool shows it as unset.
/// </summary>
public class LargeChangeVariableScenarioTests
{
    [AvaloniaTheory]
    [InlineData("Controls/ScrollBar")]
    [InlineData("Controls/ScrollViewer")]
    public void Instance_ShowsLargeChangeUnsetWithoutADefault(string type)
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave instance = FormsTemplateInstanceVariablesScenarioTests.AddStandardInstance(grid, "Instance", type);

        grid.Select(instance);

        grid.ShownMemberNames().ShouldContain("Instance.LargeChange");
        grid.ShowsSetMarker("LargeChange").ShouldBeFalse();
        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Instance.LargeChange").ShouldBeNull();
        grid.Member("LargeChange").Value.ShouldBeNull();
    }

    [AvaloniaTheory]
    [InlineData("Controls/ScrollBar")]
    [InlineData("Controls/ScrollViewer")]
    public void Instance_LargeChangeDetailText_ExplainsEmpty(string type)
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave instance = FormsTemplateInstanceVariablesScenarioTests.AddStandardInstance(grid, "Instance", type);

        grid.Select(instance);

        grid.Member("LargeChange").DetailText.ShouldBe("Step for track clicks. Empty follows the viewport size.");
    }

    [AvaloniaTheory]
    [InlineData("Controls/ScrollBar")]
    [InlineData("Controls/ScrollViewer")]
    public void Instance_TypingLargeChange_StoresADouble(string type)
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave instance = FormsTemplateInstanceVariablesScenarioTests.AddStandardInstance(grid, "Instance", type);
        ElementSave owner = grid.SelectedState.SelectedElement!;
        grid.Select(instance);

        ClearIsNull(grid);
        grid.TypeAndEnter("LargeChange", "25");

        VariableGridHarness.StoredValue(owner, "Instance.LargeChange").ShouldBe(25.0);
        grid.ShowsSetMarker("LargeChange").ShouldBeTrue();
    }

    [AvaloniaFact]
    public void Instance_MakeDefaultOnAuthoredLargeChange_ReturnsToUnset()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave instance = FormsTemplateInstanceVariablesScenarioTests.AddStandardInstance(grid, "Instance", "Controls/ScrollBar");
        ElementSave owner = grid.SelectedState.SelectedElement!;
        grid.Select(instance);
        ClearIsNull(grid);
        grid.TypeAndEnter("LargeChange", "25");

        grid.PickRowMenuItem("LargeChange", "Make Default");

        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Instance.LargeChange").ShouldBeNull();
    }

    private static void ClearIsNull(VariableGridHarness grid)
    {
        CheckBox isNull = grid.Editor<TextBoxDisplay>("LargeChange").GetVisualDescendants().OfType<CheckBox>()
            .Single(box => box.Content as string == "Is Null");
        grid.Input.Click(isNull);
    }
}
