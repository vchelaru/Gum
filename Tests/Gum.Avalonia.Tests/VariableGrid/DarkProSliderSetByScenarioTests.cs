using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.DataTypes.Serialization.Json;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// The "Set by" line and category color bar over a real saved component (a DarkPro Slider whose
/// SliderCategory sets TrackBackground's variables and VariableReferences only in later states), loaded
/// through the serializer the way a reopened project is (#7647).
/// </summary>
public class DarkProSliderSetByScenarioTests
{
    internal static ComponentSave LoadSlider(VariableGridHarness grid)
    {
        string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "VariableGrid", "Fixtures", "DarkProSlider.gucj"));
        ComponentSave loaded = GumJsonFileSerializer.DeserializeElement<ComponentSave>(json);
        grid.Project.Project.Components.Add(loaded);
        grid.Project.Project.ComponentReferences.Add(new ElementReference { Name = loaded.Name, ElementType = ElementType.Component });
        // Saving then reopening runs the file through the serializer both ways, as a user's project goes.
        grid.Project.SaveAndReload();
        ComponentSave slider = grid.Project.Project.Components.Single(c => c.Name == "DarkPro/Slider");
        slider.Categories.Single().ColorName = "Indigo";
        return slider;
    }

    internal static void Show(VariableGridHarness grid, ComponentSave slider, InstanceSave? instance)
    {
        // Selecting the same target again does not rebuild the grid, so leave the element first.
        grid.Select((ElementSave)grid.Project.Project.Components.First(c => c != slider));
        grid.Select(slider);
        if (instance != null)
        {
            grid.Select(instance);
        }
        grid.Select(slider.DefaultState);
    }

    [AvaloniaFact]
    public void TrackBackground_VariablesSetOnlyInLaterStates_SaySetBySliderCategory()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        grid.Project.AddComponent("Other");
        ComponentSave slider = LoadSlider(grid);
        InstanceSave track = slider.Instances.Single(i => i.Name == "TrackBackground");
        Show(grid, slider, track);

        InstanceMember references = grid.Member("VariableReferences");
        references.DetailText.ShouldNotBeNull().ShouldContain("Set by SliderCategory");
        references.MarkerColorName.ShouldBe("Indigo");
        // FillBlue etc. fold into one color row, whose detail text names the category.
        InstanceMember fillColor = grid.Member("FillColor");
        fillColor.DetailText.ShouldNotBeNull().ShouldContain("Set by SliderCategory");
        fillColor.MarkerColorName.ShouldBe("Indigo");
    }

    [AvaloniaFact]
    public void ElementLevelVariableReferences_SetOnlyInALaterState_SaysSetBySliderCategory()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        grid.Project.AddComponent("Other");
        ComponentSave slider = LoadSlider(grid);
        slider.DefaultState.VariableLists.Add(new VariableListSave<string> { Name = "VariableReferences", Type = "string", Value = new List<string> { "X = 1" } });
        slider.Categories.Single().States.Single(s => s.Name == "Disabled").VariableLists.Add(
            new VariableListSave<string> { Name = "VariableReferences", Type = "string", Value = new List<string> { "X = 2" } });
        Show(grid, slider, null);

        InstanceMember references = grid.Member("VariableReferences");
        references.DetailText.ShouldNotBeNull().ShouldContain("Set by SliderCategory");
        references.MarkerColorName.ShouldBe("Indigo");
    }
}
