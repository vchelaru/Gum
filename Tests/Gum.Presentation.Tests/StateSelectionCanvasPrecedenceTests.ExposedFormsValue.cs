using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

/// <summary>
/// A Forms value (IsEnabled) a component exposes from its Button instance reaches that button on the canvas
/// when an instance of the component sets it (#7641), through any number of exposures. The result is
/// computed for the in-memory canvas only; nothing is written into the saved element.
/// </summary>
public partial class StateSelectionCanvasPrecedenceTests
{
    #region Exposed Forms value on the canvas

    [Theory]
    [InlineData(false, "Disabled")]
    [InlineData(true, "Enabled")]
    public void RefreshAll_ComponentInstanceSetsExposedIsEnabled_InnerButtonShowsTheFormsDerivedLook(bool isEnabled, string expectedLook)
    {
        ScreenSave screen = BuildScreenWithRowInstance(exposeThroughWrapper: false, isEnabled);

        RowButtonColor(Show(screen, screen.DefaultState!), wrapped: false).ShouldBe(ButtonColors[expectedLook]);
    }

    [Theory]
    [InlineData(false, "Disabled")]
    [InlineData(true, "Enabled")]
    public void RefreshAll_ExposedTwiceThroughANestedComponent_InnerButtonShowsTheFormsDerivedLook(bool isEnabled, string expectedLook)
    {
        ScreenSave screen = BuildScreenWithRowInstance(exposeThroughWrapper: true, isEnabled);

        RowButtonColor(Show(screen, screen.DefaultState!), wrapped: true).ShouldBe(ButtonColors[expectedLook]);
    }

    [Fact]
    public void RefreshAll_ScreenStateSetsExposedIsEnabled_InnerButtonShowsDisabledWhileThatStateIsSelected()
    {
        ScreenSave screen = BuildScreenWithRowInstance(exposeThroughWrapper: false, isEnabled: true);
        StateSave off = AddCategoryState(screen, "ModeCategory", "Off", s =>
            s.Variables.Add(new VariableSave { Name = "Row1.IsEnabled", Value = false, Type = "bool", SetsValue = true }));

        RowButtonColor(Show(screen, off), wrapped: false).ShouldBe(ButtonColors["Disabled"]);
        RowButtonColor(Show(screen, screen.DefaultState!), wrapped: false).ShouldBe(ButtonColors["Enabled"]);
    }

    [Fact]
    public void RefreshAll_ExposureRemoved_InnerButtonIgnoresTheInstanceValue()
    {
        ScreenSave screen = BuildScreenWithRowInstance(exposeThroughWrapper: false, isEnabled: false);
        ComponentSave row = ObjectFinder.Self.GumProjectSave!.Components.Single(item => item.Name == "Row");
        row.DefaultState!.Variables.Single(item => item.Name == "Play.IsEnabled").ExposedAsName = null;

        RowButtonColor(Show(screen, screen.DefaultState!), wrapped: false).ShouldBe(ButtonColors["Enabled"]);
    }

    [Fact]
    public void RefreshAll_ComponentInstanceSetsExposedIsEnabled_NothingIsWrittenIntoTheSavedScreen()
    {
        ScreenSave screen = BuildScreenWithRowInstance(exposeThroughWrapper: false, isEnabled: false);
        string[] before = screen.DefaultState!.Variables.Select(item => item.Name).ToArray();

        Show(screen, screen.DefaultState!);

        screen.DefaultState!.Variables.Select(item => item.Name).ToArray().ShouldBe(before);
    }

    private static (int, int, int) RowButtonColor(GraphicalUiElement root, bool wrapped)
    {
        GraphicalUiElement row = FindByName(root, wrapped ? "Outer1" : "Row1");
        GraphicalUiElement play = FindByName(wrapped ? FindByName(row, "Row1") : row, "Play");
        RecordingRenderable renderable = (RecordingRenderable)FindByName(play, "Background").RenderableComponent!;
        return (renderable.Red, renderable.Green, renderable.Blue);
    }

    /// <summary>Row holds a Button as Play and exposes its IsEnabled; Outer (optional) holds a Row and exposes that again.</summary>
    private static ScreenSave BuildScreenWithRowInstance(bool exposeThroughWrapper, bool isEnabled)
    {
        BuildFullButton(defaultReference: true, materialized: true);
        GumProjectSave project = ObjectFinder.Self.GumProjectSave!;

        ComponentSave row = new ComponentSave { Name = "Row", BaseType = "Container" };
        StateSave rowDefault = new StateSave { Name = "Default", ParentContainer = row };
        row.States.Add(rowDefault);
        row.Instances.Add(new InstanceSave { Name = "Play", BaseType = "Controls/ButtonStandard", ParentContainer = row });
        rowDefault.Variables.Add(new VariableSave { Name = "Play.IsEnabled", Type = "bool", Value = true, SetsValue = true, ExposedAsName = "IsEnabled" });
        project.Components.Add(row);

        ScreenSave screen = new ScreenSave { Name = "Options" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        project.Screens.Add(screen);

        if (exposeThroughWrapper)
        {
            ComponentSave outer = new ComponentSave { Name = "Outer", BaseType = "Container" };
            StateSave outerDefault = new StateSave { Name = "Default", ParentContainer = outer };
            outer.States.Add(outerDefault);
            outer.Instances.Add(new InstanceSave { Name = "Row1", BaseType = "Row", ParentContainer = outer });
            outerDefault.Variables.Add(new VariableSave { Name = "Row1.IsEnabled", Type = "bool", SetsValue = false, ExposedAsName = "IsEnabled" });
            project.Components.Add(outer);
            screen.Instances.Add(new InstanceSave { Name = "Outer1", BaseType = "Outer", ParentContainer = screen });
            screenDefault.Variables.Add(new VariableSave { Name = "Outer1.IsEnabled", Type = "bool", Value = isEnabled, SetsValue = true });
        }
        else
        {
            screen.Instances.Add(new InstanceSave { Name = "Row1", BaseType = "Row", ParentContainer = screen });
            screenDefault.Variables.Add(new VariableSave { Name = "Row1.IsEnabled", Type = "bool", Value = isEnabled, SetsValue = true });
        }

        return screen;
    }

    #endregion
}
