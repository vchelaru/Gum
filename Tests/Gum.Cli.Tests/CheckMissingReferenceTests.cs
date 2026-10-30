using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// #7119: variables that name an instance, a variable or a type the project does not have.
/// </summary>
public class CheckMissingReferenceTests : CheckHandWrittenFileHarness
{
    #region GUM0027 — Variable names an instance the element does not have

    [Fact]
    public void Check_VariableOfInstanceThatDoesNotExist_ShouldReportGum0027()
    {
        string project = CreateProject("GhostInstanceVariable");
        AddComponent(project, "Holder", "Container", DefaultState(Variable("string", "Ghost.Text", "string", "hi")));

        CheckRun run = Check(project);

        CheckEntry entry = run.Entries.ShouldHaveSingleItem();
        entry.Code.ShouldBe("GUM0027");
        entry.Severity.ShouldBe("Error");
        entry.Element.ShouldBe("Holder");
        entry.Message.ShouldContain("Ghost");
        run.ExitCode.ShouldBe(1);
    }

    [Fact]
    public void Check_VariableOfInstanceDefinedInTheElement_ShouldNotReportGum0027()
    {
        string project = CreateProject("OwnInstanceVariable");
        AddComponent(project, "Holder", "Container",
            DefaultState(Variable("string", "Label.Text", "string", "hi")) + Instance("Label", "Text"));

        Check(project).Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Check_VariableOfInstanceDefinedInABaseElement_ShouldNotReportGum0027()
    {
        string project = CreateProject("BaseInstanceVariable");
        AddComponent(project, "Root", "Container", DefaultState() + Instance("Label", "Text"));
        AddComponent(project, "Middle", "Root", DefaultState());
        AddComponent(project, "Leaf", "Middle", DefaultState(Variable("string", "Label.Text", "string", "hi")));

        Check(project).Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Check_ExposedVariableAndStateVariableOfExistingInstance_ShouldNotReportGum0027()
    {
        string project = CreateProject("ExposedAndState");
        AddComponent(project, "Toggle", "Container",
            DefaultState() + "<Category><Name>Mode</Name><State><Name>On</Name></State></Category>");
        AddComponent(project, "Holder", "Container",
            DefaultState(
                Variable("string", "Label.Text", "string", "hi", exposedAs: "Caption"),
                Variable("Mode", "Switch.ModeState", "string", "On"))
            + Instance("Label", "Text") + Instance("Switch", "Toggle"));

        Check(project).Entries.ShouldBeEmpty();
    }

    #endregion

    #region GUM0028 — Variable with a type nothing can resolve

    [Fact]
    public void Check_VariableWithUnknownTypeName_ShouldReportGum0028AsWarning()
    {
        string project = CreateProject("UnknownVariableType");
        AddComponent(project, "Holder", "Container",
            DefaultState(Variable("Blorp", "Label.Whatever", "string", "x")) + Instance("Label", "Text"));

        CheckRun run = Check(project);

        CheckEntry entry = run.Entries.Single(e => e.Code == "GUM0028");
        entry.Severity.ShouldBe("Warning");
        entry.Element.ShouldBe("Holder");
        entry.Message.ShouldContain("Blorp");
    }

    [Fact]
    public void Check_VariableTypedWithGumCommonEnumOrCategoryName_ShouldNotReportGum0028()
    {
        string project = CreateProject("KnownVariableTypes");
        AddComponent(project, "Toggle", "Container",
            DefaultState() + "<Category><Name>ColorCategory</Name><State><Name>On</Name></State></Category>");
        AddComponent(project, "Holder", "Container",
            DefaultState(
                Variable("GradientType", "Label.GradientType", "string", "Linear"),
                Variable("Orientation?", "Label.Orientation", "string", "Horizontal"),
                Variable("ColorCategory", "Label.Tint", "string", "On"),
                Variable("Missing", "Label.SomethingState", "string", "On"))
            + Instance("Label", "Text"));

        Check(project).Entries.ShouldNotContain(e => e.Code == "GUM0028");
    }

    #endregion
}
