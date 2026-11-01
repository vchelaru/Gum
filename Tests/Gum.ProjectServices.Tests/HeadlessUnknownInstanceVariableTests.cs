using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// #7210: GUM0029, a variable set on an instance whose type does not have it.
/// </summary>
public class HeadlessUnknownInstanceVariableTests : BaseTestClass
{
    private ScreenSave AddScreenWithLabel(string variableName, string type = "string", object? value = null, string labelType = "Text")
    {
        ScreenSave screen = new ScreenSave { Name = "Title" };
        screen.Instances.Add(new InstanceSave { Name = "Label", BaseType = labelType, ParentContainer = screen });
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        state.Variables.Add(new VariableSave { Name = variableName, Type = type, Value = value ?? "x", SetsValue = true });
        screen.States.Add(state);
        Project.Screens.Add(screen);
        return screen;
    }

    private static HeadlessErrorChecker CreateChecker(bool reportsLeftovers = true) =>
        new HeadlessErrorChecker(new DefaultTypeResolver()) { ReportsLeftoverInstanceVariables = reportsLeftovers };

    [Fact]
    public void GetErrorsFor_VariableTheInstanceTypeLacks_ShouldReportGum0029Warning()
    {
        ScreenSave screen = AddScreenWithLabel("Label.NotAVariable");

        ErrorResult error = CreateChecker().GetErrorsFor(screen, Project).Single(e => e.Code == "GUM0029");

        error.Severity.ShouldBe(ErrorSeverity.Warning);
        error.ElementName.ShouldBe("Title");
        error.Message.ShouldContain("Label.NotAVariable");
        error.Message.ShouldContain("Text");
    }

    [Fact]
    public void GetErrorsFor_VariableTheInstanceTypeHas_ShouldNotReportGum0029()
    {
        ScreenSave screen = AddScreenWithLabel("Label.Text");

        CreateChecker().GetErrorsFor(screen, Project).ShouldNotContain(e => e.Code == "GUM0029");
    }

    [Fact]
    public void GetErrorsFor_UnknownVariableByDefault_ShouldNotReportGum0029()
    {
        ScreenSave screen = AddScreenWithLabel("Label.NotAVariable");

        CreateChecker(reportsLeftovers: false).GetErrorsFor(screen, Project).ShouldNotContain(e => e.Code == "GUM0029");
    }

    // The runtime ignores a selector for a category the type no longer has, and the tool leaves it behind.
    [Fact]
    public void GetErrorsFor_StaleCategorySelector_ShouldNotReportGum0029()
    {
        ScreenSave screen = AddScreenWithLabel("Label.SizeCategoryState", type: "SizeCategory", value: "Big");

        CreateChecker().GetErrorsFor(screen, Project).ShouldNotContain(e => e.Code == "GUM0029");
    }

    // A component that has a Forms behavior is driven by a Forms control, which receives variables the
    // variable lists do not name (IsChecked, Orientation).
    [Fact]
    public void GetErrorsFor_InstanceOfComponentWithFormsBehavior_ShouldNotReportGum0029()
    {
        ComponentSave check = new ComponentSave { Name = "Check", BaseType = "Container" };
        check.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "CheckBoxBehavior" });
        check.States.Add(new StateSave { Name = "Default", ParentContainer = check });
        Project.Components.Add(check);
        ScreenSave screen = AddScreenWithLabel("Label.IsChecked", type: "bool", value: true, labelType: "Check");

        CreateChecker().GetErrorsFor(screen, Project).ShouldNotContain(e => e.Code == "GUM0029");
    }

    [Fact]
    public void GetErrorsFor_UnknownVariableOnPlainComponent_ShouldReportGum0029()
    {
        ComponentSave plain = new ComponentSave { Name = "Plain", BaseType = "Container" };
        plain.States.Add(new StateSave { Name = "Default", ParentContainer = plain });
        Project.Components.Add(plain);
        ScreenSave screen = AddScreenWithLabel("Label.NotAVariable", labelType: "Plain");

        CreateChecker().GetErrorsFor(screen, Project).ShouldContain(e => e.Code == "GUM0029");
    }

    [Fact]
    public void GetErrorsFor_VariableExposedByTheInstancesComponent_ShouldNotReportGum0029()
    {
        ComponentSave plain = new ComponentSave { Name = "Plain", BaseType = "Container" };
        StateSave plainState = new StateSave { Name = "Default", ParentContainer = plain };
        plain.Instances.Add(new InstanceSave { Name = "Container", BaseType = "Text", ParentContainer = plain });
        plainState.Variables.Add(new VariableSave { Name = "Container.Text", Type = "string", Value = "", SetsValue = true, ExposedAsName = "Caption" });
        plain.States.Add(plainState);
        Project.Components.Add(plain);
        ScreenSave screen = AddScreenWithLabel("Label.Caption", labelType: "Plain");

        CreateChecker().GetErrorsFor(screen, Project).ShouldNotContain(e => e.Code == "GUM0029");
    }

    [Fact]
    public void GetErrorsFor_SameUnknownVariableInSeveralStates_ShouldReportGum0029Once()
    {
        ScreenSave screen = AddScreenWithLabel("Label.NotAVariable");
        StateSave other = new StateSave { Name = "Other", ParentContainer = screen };
        other.Variables.Add(new VariableSave { Name = "Label.NotAVariable", Type = "string", Value = "y", SetsValue = true });
        screen.States.Add(other);

        CreateChecker().GetErrorsFor(screen, Project).Count(e => e.Code == "GUM0029").ShouldBe(1);
    }

    // DropshadowBlurX/Y were replaced by DropshadowBlur (#2950); old projects still hold them and they do nothing.
    [Fact]
    public void GetErrorsFor_RemovedStandardVariable_ShouldNotReportGum0029()
    {
        ScreenSave screen = AddScreenWithLabel("Label.DropshadowBlurX", type: "float", value: 2f, labelType: "ColoredRectangle");

        CreateChecker().GetErrorsFor(screen, Project).ShouldNotContain(e => e.Code == "GUM0029");
    }

    // Older projects wrote a category selector without the State suffix.
    [Fact]
    public void GetErrorsFor_CategorySelectorWithoutStateSuffix_ShouldNotReportGum0029()
    {
        ComponentSave icon = new ComponentSave { Name = "Icon", BaseType = "Container" };
        icon.States.Add(new StateSave { Name = "Default", ParentContainer = icon });
        icon.Categories.Add(new StateSaveCategory { Name = "IconCategory" });
        Project.Components.Add(icon);
        ScreenSave screen = AddScreenWithLabel("Label.IconCategory", type: "IconCategory", value: "Close", labelType: "Icon");

        CreateChecker().GetErrorsFor(screen, Project).ShouldNotContain(e => e.Code == "GUM0029");
    }
}
