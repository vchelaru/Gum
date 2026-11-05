using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// A composite row (StrokeColor = StrokeRed/FillGreen/StrokeBlue) shows the same category color bar a plain row
/// does: the first colored category, in category order, whose states set any of its channels (#7661).
/// </summary>
public class CompositeColorBarScenarioTests
{
    private static (ComponentSave Component, InstanceSave Track) Build(VariableGridHarness grid)
    {
        ComponentSave component = grid.Project.AddComponent("Slider");
        InstanceSave track = grid.Project.AddInstance(component, "Track", "Rectangle");
        // A category state only counts for a variable the default state also holds.
        foreach (string channel in new[] { "StrokeRed", "StrokeGreen", "StrokeBlue" })
        {
            component.GetDefaultStateOrThrow().SetValue("Track." + channel, 0, "int");
        }
        return (component, track);
    }

    private static StateSave AddCategoryState(VariableGridHarness grid, ComponentSave component, string categoryName,
        string? colorName, params string[] stateNames)
    {
        StateSaveCategory category = grid.Project.AddCategory(component, categoryName);
        category.ColorName = colorName;
        StateSave? last = null;
        foreach (string name in stateNames)
        {
            last = grid.Project.AddState(component, category, name);
        }
        return last!;
    }

    private static void Show(VariableGridHarness grid, ComponentSave component, InstanceSave track)
    {
        grid.Select(component);
        grid.Select(track);
        grid.Select(component.GetDefaultStateOrThrow());
    }

    [AvaloniaFact]
    public void CompositeRow_ChannelSetInNonFirstStateOfColoredCategory_ShowsTheBar()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave component, InstanceSave track) = Build(grid);
        StateSave disabled = AddCategoryState(grid, component, "SliderCategory", "Indigo", "Enabled", "Disabled");
        disabled.SetValue("Track.StrokeBlue", 10, "int");
        Show(grid, component, track);

        grid.Member("StrokeColor").MarkerColorName.ShouldBe("Indigo");
    }

    [AvaloniaFact]
    public void CompositeRow_TwoColoredCategories_FirstInCategoryOrderWins()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave component, InstanceSave track) = Build(grid);
        StateSave first = AddCategoryState(grid, component, "First", "Indigo", "A");
        StateSave second = AddCategoryState(grid, component, "Second", "Rose", "B");
        // The later category sets the channel listed first; the earlier one sets a later channel.
        second.SetValue("Track.StrokeRed", 10, "int");
        first.SetValue("Track.StrokeBlue", 10, "int");
        Show(grid, component, track);

        grid.Member("StrokeColor").MarkerColorName.ShouldBe("Indigo");
    }

    [AvaloniaFact]
    public void CompositeRow_UncoloredCategory_ShowsNoBar()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave component, InstanceSave track) = Build(grid);
        StateSave state = AddCategoryState(grid, component, "SliderCategory", null, "Disabled");
        state.SetValue("Track.StrokeBlue", 10, "int");
        Show(grid, component, track);

        grid.Member("StrokeColor").MarkerColorName.ShouldBeNull();
    }

    [AvaloniaFact]
    public void CompositeRow_ChannelSetOnlyInDefault_ShowsNoBar()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave component, InstanceSave track) = Build(grid);
        AddCategoryState(grid, component, "SliderCategory", "Indigo", "Disabled");
        component.GetDefaultStateOrThrow().SetValue("Track.StrokeBlue", 10, "int");
        Show(grid, component, track);

        grid.Member("StrokeColor").MarkerColorName.ShouldBeNull();
    }

    [AvaloniaFact]
    public void PlainRow_SetByColoredCategory_StillShowsTheBar_AndUnrelatedRowDoesNot()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave component, InstanceSave track) = Build(grid);
        StateSave disabled = AddCategoryState(grid, component, "SliderCategory", "Indigo", "Disabled");
        disabled.SetValue("Track.Width", 50f, "float");
        Show(grid, component, track);

        grid.Member("Width").MarkerColorName.ShouldBe("Indigo");
        grid.Member("Height").MarkerColorName.ShouldBeNull();
        grid.Member("StrokeColor").MarkerColorName.ShouldBeNull();
    }
}
