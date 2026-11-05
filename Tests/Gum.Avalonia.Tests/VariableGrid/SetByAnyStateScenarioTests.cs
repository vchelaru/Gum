using Gum.DataTypes.Variables;
using WpfDataUi.DataTypes;
using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.Avalonia.Tests.Harness;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// The Variables tab's "Set by" line and category color bar name a category when any of its states
/// sets the variable, list variables included (#7647).
/// </summary>
public class SetByAnyStateScenarioTests
{
    /// <summary>
    /// A Slider with a Track instance. SliderCategory (colored) has Enabled, Disabled and DisabledFocused;
    /// only Disabled sets Track's VariableReferences and Width, and Default sets both too.
    /// </summary>
    internal static (ComponentSave Slider, InstanceSave Track, StateSaveCategory Category) BuildSlider(ToolProjectFixture project)
    {
        ComponentSave slider = project.AddComponent("Slider");
        InstanceSave track = project.AddInstance(slider, "Track", "Rectangle");
        StateSaveCategory category = project.AddCategory(slider, "SliderCategory");
        category.ColorName = "Indigo";
        project.AddState(slider, category, "Enabled");
        StateSave disabled = project.AddState(slider, category, "Disabled");
        project.AddState(slider, category, "DisabledFocused");

        slider.GetDefaultStateOrThrow().SetValue("Track.Width", 100f, "float");
        slider.GetDefaultStateOrThrow().VariableLists.Add(
            new VariableListSave<string> { Name = "Track.VariableReferences", Type = "string", Value = new List<string> { "Red = Track.Green" } });
        disabled.SetValue("Track.Width", 50f, "float");
        disabled.VariableLists.Add(
            new VariableListSave<string> { Name = "Track.VariableReferences", Type = "string", Value = new List<string> { "Red = Track.Blue" } });
        return (slider, track, category);
    }

    [AvaloniaFact]
    public void VariableReferencesRow_SetOnlyByALaterState_SaysSetByAndShowsTheColorBar()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave slider, InstanceSave track, _) = BuildSlider(grid.Project);
        grid.Select(slider);
        grid.Select(track);
        grid.Select(slider.GetDefaultStateOrThrow());

        InstanceMember member = grid.Member("VariableReferences");
        member.DetailText.ShouldNotBeNull().ShouldContain("Set by SliderCategory");
        member.MarkerColorName.ShouldBe("Indigo");
    }

    [AvaloniaFact]
    public void PlainRow_SetOnlyByALaterState_SaysSetByAndShowsTheColorBar()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave slider, InstanceSave track, _) = BuildSlider(grid.Project);
        grid.Select(slider);
        grid.Select(track);
        grid.Select(slider.GetDefaultStateOrThrow());

        InstanceMember member = grid.Member("Width");
        member.DetailText.ShouldNotBeNull().ShouldContain("Set by SliderCategory");
        member.MarkerColorName.ShouldBe("Indigo");
    }
}
