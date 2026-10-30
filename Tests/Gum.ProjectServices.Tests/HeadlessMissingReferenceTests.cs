using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// #7119: GUM0027 and GUM0028 must not appear for what the Gum tool itself saves.
/// </summary>
public class HeadlessMissingReferenceTests : BaseTestClass
{
    private ComponentSave AddComponentWithLeftoverVariable()
    {
        ComponentSave component = new ComponentSave { Name = "IconButton", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        state.Variables.Add(new VariableSave { Name = "Label.Text", Type = "string", Value = "hi", SetsValue = true });
        component.States.Add(state);
        Project.Components.Add(component);
        return component;
    }

    [Fact]
    public void GetErrorsFor_VariableOfMissingInstance_ShouldNotReportGum0027ByDefault()
    {
        ComponentSave component = AddComponentWithLeftoverVariable();
        HeadlessErrorChecker sut = new HeadlessErrorChecker(new DefaultTypeResolver());

        sut.GetErrorsFor(component, Project).ShouldNotContain(e => e.Code == "GUM0027");
        sut.GetAllErrors(Project).ShouldNotContain(e => e.Code == "GUM0027");
    }

    [Fact]
    public void GetErrorsFor_VariableOfMissingInstance_ShouldReportGum0027WarningWhenRequested()
    {
        ComponentSave component = AddComponentWithLeftoverVariable();
        HeadlessErrorChecker sut = new HeadlessErrorChecker(new DefaultTypeResolver()) { ReportsLeftoverInstanceVariables = true };

        ErrorResult error = sut.GetErrorsFor(component, Project).Single(e => e.Code == "GUM0027");

        error.Severity.ShouldBe(ErrorSeverity.Warning);
    }

    // The type of an exposed variable is the type of what it exposes. After the category it exposes is renamed, the
    // tool leaves the old name on the instance's variable.
    [Fact]
    public void GetErrorsFor_ExposedStateVariableWithRenamedCategoryType_ShouldNotReportGum0028()
    {
        ComponentSave icon = new ComponentSave { Name = "Icon", BaseType = "Container" };
        icon.States.Add(new StateSave { Name = "Default", ParentContainer = icon });
        icon.Categories.Add(new StateSaveCategory { Name = "Shape" });

        ComponentSave button = new ComponentSave { Name = "Button", BaseType = "Container" };
        button.Instances.Add(new InstanceSave { Name = "Glyph", BaseType = "Icon" });
        StateSave buttonState = new StateSave { Name = "Default", ParentContainer = button };
        buttonState.Variables.Add(new VariableSave
        {
            Name = "Glyph.ShapeState", Type = "Shape", Value = null, SetsValue = true, ExposedAsName = "GlyphKind"
        });
        button.States.Add(buttonState);

        ScreenSave title = new ScreenSave { Name = "Title" };
        title.Instances.Add(new InstanceSave { Name = "OkButton", BaseType = "Button" });
        StateSave titleState = new StateSave { Name = "Default", ParentContainer = title };
        titleState.Variables.Add(new VariableSave { Name = "OkButton.GlyphKind", Type = "Kind", Value = "Square", SetsValue = true });
        title.States.Add(titleState);

        Project.Components.Add(icon);
        Project.Components.Add(button);
        Project.Screens.Add(title);

        new HeadlessErrorChecker(new DefaultTypeResolver()).GetErrorsFor(title, Project)
            .ShouldNotContain(e => e.Code == "GUM0028");
    }
}
