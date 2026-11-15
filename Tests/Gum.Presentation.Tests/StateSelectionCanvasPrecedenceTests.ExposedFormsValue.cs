using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
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

public partial class StateSelectionCanvasPrecedenceTests
{
    #region Exposure inherited from a base component

    [Theory]
    [InlineData(false, "Disabled")]
    [InlineData(true, "Enabled")]
    public void RefreshAll_InstanceOfDerivedComponentSetsInheritedExposedIsEnabled_InnerButtonShowsTheFormsDerivedLook(bool isEnabled, string expectedLook)
    {
        ScreenSave screen = BuildScreenWithRowInstance(exposeThroughWrapper: false, isEnabled);
        AddDerivedRow(screen, exposedNameOverride: null);

        RowButtonColor(Show(screen, screen.DefaultState!), wrapped: false).ShouldBe(ButtonColors[expectedLook]);
    }

    [Fact]
    public void RefreshAll_DerivedComponentReexposesTheValueUnderItsOwnName_ItsNameWins()
    {
        ScreenSave screen = BuildScreenWithRowInstance(exposeThroughWrapper: false, isEnabled: true);
        AddDerivedRow(screen, exposedNameOverride: "Active");
        screen.DefaultState!.Variables.Add(new VariableSave { Name = "Row1.Active", Type = "bool", Value = false, SetsValue = true });

        RowButtonColor(Show(screen, screen.DefaultState!), wrapped: false).ShouldBe(ButtonColors["Disabled"]);
    }

    /// <summary>Row1 becomes an instance of RowDerived, a component with no instances of its own that derives from Row.</summary>
    private static void AddDerivedRow(ScreenSave screen, string? exposedNameOverride)
    {
        GumProjectSave project = ObjectFinder.Self.GumProjectSave!;
        ComponentSave derived = new ComponentSave { Name = "RowDerived", BaseType = "Row" };
        StateSave derivedDefault = new StateSave { Name = "Default", ParentContainer = derived };
        derived.States.Add(derivedDefault);
        // A derived component lists the instances its base defines, flagged DefinedByBase.
        derived.Instances.Add(new InstanceSave { Name = "Play", BaseType = "Controls/ButtonStandard", DefinedByBase = true, ParentContainer = derived });
        if (exposedNameOverride != null)
        {
            derivedDefault.Variables.Add(new VariableSave { Name = "Play.IsEnabled", Type = "bool", Value = true, SetsValue = true, ExposedAsName = exposedNameOverride });
        }
        project.Components.Add(derived);
        screen.Instances.Single(item => item.Name == "Row1").BaseType = "RowDerived";
    }

    #endregion
}

public partial class StateSelectionCanvasPrecedenceTests
{
    #region Saved data and other controls

    /// <summary>The canvas computes the nested button's look in memory; the files the tool saves are untouched by it.</summary>
    [Fact]
    public void RefreshAll_ComponentInstanceSetsExposedIsEnabled_SavedFilesAreByteIdenticalAfterTheCanvasApplies()
    {
        ScreenSave screen = BuildScreenWithRowInstance(exposeThroughWrapper: true, isEnabled: false);
        ComponentSave outer = ObjectFinder.Self.GumProjectSave!.Components.Single(item => item.Name == "Outer");
        ComponentSave row = ObjectFinder.Self.GumProjectSave!.Components.Single(item => item.Name == "Row");
        string folder = Path.Combine(Path.GetTempPath(), "gum7641-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string[] Save(string suffix)
            {
                screen.Save(Path.Combine(folder, "screen" + suffix + ".gusj"));
                outer.Save(Path.Combine(folder, "outer" + suffix + ".gucj"));
                row.Save(Path.Combine(folder, "row" + suffix + ".gucj"));
                return new[] { "screen", "outer", "row" }
                    .Select(name => File.ReadAllText(Path.Combine(folder, name + suffix + (name == "screen" ? ".gusj" : ".gucj"))))
                    .ToArray();
            }

            string[] before = Save("A");
            RowButtonColor(Show(screen, screen.DefaultState!), wrapped: true).ShouldBe(ButtonColors["Disabled"]);
            string[] after = Save("B");

            after.ShouldBe(before);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>A CheckBox whose behavior reads two Forms values: the one an instance supplies through exposure and a default.</summary>
    [Fact]
    public void GetValuesThroughExposedVariables_ExposedIsEnabledOnACheckBox_DrivesItsCategoryStateFromTheDeclaredDefaults()
    {
        BuildFullButton(defaultReference: true, materialized: true);
        GumProjectSave project = ObjectFinder.Self.GumProjectSave!;
        BehaviorSave behavior = new BehaviorSave { Name = "CheckBoxBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsChecked", Value = false });
        behavior.ToolOnlyVariableReferences.Add("CheckBoxCategoryState = IsEnabled ? (IsChecked ? \"EnabledOn\" : \"EnabledOff\") : (IsChecked ? \"DisabledOn\" : \"DisabledOff\")");
        project.Behaviors.Add(behavior);

        ComponentSave checkBox = new ComponentSave { Name = "Controls/CheckBox", BaseType = "Container" };
        checkBox.States.Add(new StateSave { Name = "Default", ParentContainer = checkBox });
        checkBox.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "CheckBoxBehavior" });
        project.Components.Add(checkBox);

        ComponentSave row = new ComponentSave { Name = "SettingRow", BaseType = "Container" };
        StateSave rowDefault = new StateSave { Name = "Default", ParentContainer = row };
        row.States.Add(rowDefault);
        row.Instances.Add(new InstanceSave { Name = "Box", BaseType = "Controls/CheckBox", ParentContainer = row });
        rowDefault.Variables.Add(new VariableSave { Name = "Box.IsEnabled", Type = "bool", SetsValue = false, ExposedAsName = "IsEnabled" });
        rowDefault.Variables.Add(new VariableSave { Name = "Box.IsChecked", Type = "bool", SetsValue = false, ExposedAsName = "IsChecked" });
        project.Components.Add(row);

        ScreenSave screen = new ScreenSave { Name = "Settings" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        screen.Instances.Add(new InstanceSave { Name = "Setting1", BaseType = "SettingRow", ParentContainer = screen });
        screenDefault.Variables.Add(new VariableSave { Name = "Setting1.IsEnabled", Type = "bool", Value = false, SetsValue = true });

        StateSave result = BehaviorToolOnlyReferencesApplier.GetValuesThroughExposedVariables(screen, selectedState: null);

        result.Variables.Single().Name.ShouldBe("Setting1.Box.CheckBoxCategoryState");
        result.Variables.Single().Value.ShouldBe("DisabledOff");
    }

    #endregion
}
