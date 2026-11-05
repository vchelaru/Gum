using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// GUM0006 for a custom font file reached through a nested component, set in the default state or in
/// a non-default state of the checked screen (#7623). The state walk skips states that set nothing
/// the component passes down; these pin that the ones that do still report.
/// </summary>
public class NestedCustomFontErrorTests : BaseTestClass
{
    private readonly HeadlessErrorChecker _sut = new HeadlessErrorChecker(new Moq.Mock<ITypeResolver>().Object);

    [Fact]
    public void GetErrorsFor_ReportsAMissingCustomFont_ASetByANonDefaultStateThroughAnExposedVariable()
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumNestedFont_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            ScreenSave screen = BuildScreen(folder, out StateSaveCategory category);
            category.States.Add(BuildState(screen, "Custom", ("Start.UseCustom", true, "bool"), ("Start.FontFile", "Fonts/Missing.ttf", "string")));
            category.States.Add(BuildState(screen, "Moved", ("Start.X", 10f, "float"), ("Start.Visible", false, "bool")));

            IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(screen, Project);

            ErrorResult error = errors.ShouldHaveSingleItem();
            error.Code.ShouldBe("GUM0006");
            error.Message.ShouldContain("Fonts/Missing.ttf");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void GetErrorsFor_ReportsNothing_WhenNoStateSetsACustomFont()
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumNestedFont_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            ScreenSave screen = BuildScreen(folder, out StateSaveCategory category);
            category.States.Add(BuildState(screen, "Moved", ("Start.X", 10f, "float"), ("Start.Visible", false, "bool")));

            _sut.GetErrorsFor(screen, Project).ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private ScreenSave BuildScreen(string folder, out StateSaveCategory category)
    {
        Project.FullFileName = Path.Combine(folder, "Project.gumx");
        StateSave textDefaults = Project.StandardElements.Single(item => item.Name == "Text").DefaultState;
        textDefaults.Variables.Add(new VariableSave { Name = "Font", Type = "string", Value = "Arial", SetsValue = true });
        textDefaults.Variables.Add(new VariableSave { Name = "FontSize", Type = "int", Value = 18, SetsValue = true });

        ComponentSave button = new ComponentSave { Name = "Button", BaseType = "Container" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        button.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = button });
        button.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Label.UseCustomFont", Type = "bool", Value = false, SetsValue = true, ExposedAsName = "UseCustom"
        });
        button.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Label.CustomFontFile", Type = "string", Value = "", SetsValue = true, ExposedAsName = "FontFile"
        });
        Project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "Main" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Start", BaseType = "Button", ParentContainer = screen });
        category = new StateSaveCategory { Name = "Layout" };
        screen.Categories.Add(category);
        Project.Screens.Add(screen);
        return screen;
    }

    private static StateSave BuildState(ScreenSave screen, string name, params (string Name, object Value, string Type)[] variables)
    {
        StateSave state = new StateSave { Name = name, ParentContainer = screen };
        foreach ((string variableName, object value, string type) in variables)
        {
            state.Variables.Add(new VariableSave { SetsValue = true, Name = variableName, Value = value, Type = type });
        }
        return state;
    }
}
