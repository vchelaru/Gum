using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// Splash damage around #7059: tests that are red on main. Each one is a bug, filed as its own issue.
/// </summary>
public class CheckHandWrittenRedTests : CheckHandWrittenFileHarness
{
    private static string Default(params string[] variables) => DefaultState(variables);

    [Fact]
    public void Check_VariableOfInstanceThatDoesNotExist_ShouldReportError()
    {
        string project = CreateProject("GhostInstanceVariable");
        AddComponent(project, "Holder", "Container", Default(Variable("string", "Ghost.Text", "string", "hi")));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Holder" && e.Message.Contains("Ghost"));
    }

    [Fact]
    public void Check_TwoInstancesWhoseParentsPointAtEachOther_ShouldReportError()
    {
        string project = CreateProject("ParentCycle");
        AddComponent(project, "Holder", "Container",
            Default(
                Variable("string", "A.Parent", "string", "B"),
                Variable("string", "B.Parent", "string", "A")) +
            Instance("A", "Container") + Instance("B", "Container"));

        Check(project).ExitCode.ShouldBe(1);
    }

    [Fact]
    public void Check_NumericVariableWithTextValue_ShouldReportError()
    {
        string project = CreateProject("WrongValueType");
        AddComponent(project, "Holder", "Container",
            Default(Variable("float", "Width", "string", "wide")));

        Check(project).ExitCode.ShouldBe(1);
    }

    [Fact]
    public void Check_DuplicateInstanceNamesInOneComponent_ShouldReportError()
    {
        string project = CreateProject("DuplicateInstances");
        AddComponent(project, "Twins", "Container", Default() + Instance("Child", "Text") + Instance("Child", "Sprite"));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Twins" && e.Message.Contains("Child"));
    }

    [Fact]
    public void Check_UnknownVariableOnAnInstance_ShouldReportAtLeastAWarning()
    {
        string project = CreateProject("UnknownInstanceVariable");
        AddComponent(project, "Holder", "Container",
            Default(Variable("string", "Label.NotAVariable", "string", "x")) + Instance("Label", "Text"));

        Check(project).Entries.ShouldContain(e => e.Element == "Holder" && e.Message.Contains("NotAVariable"));
    }

    [Fact]
    public void Check_SameComponentReferencedTwice_ShouldReportDuplicateError()
    {
        string project = CreateProject("DuplicateReference");
        AddComponent(project, "Twin", "Container", Default());
        AddReference(project, """  <ComponentReference Name="Twin" />""");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Twin");
    }

    [Fact]
    public void Check_ScreenUsedAsInstanceBaseType_ShouldReportError()
    {
        string project = CreateProject("ScreenAsInstance");
        AddScreen(project, "Menu", null, Default());
        AddComponent(project, "Holder", "Container", Default() + Instance("Embedded", "Menu"));

        Check(project).ExitCode.ShouldBe(1);
    }

    [Fact]
    public void Check_InstanceSetsAStateThatTheCategoryDoesNotHave_ShouldReportError()
    {
        string project = CreateProject("UnknownState");
        AddComponent(project, "Toggle", "Container",
            Default() + "<Category><Name>Mode</Name><State><Name>On</Name></State><State><Name>Off</Name></State></Category>");
        AddScreen(project, "Menu", null,
            Default(Variable("Mode", "Switch.ModeState", "string", "Nope")) + Instance("Switch", "Toggle"));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Message.Contains("Nope"));
    }

    [Fact]
    public void Check_VariableWithUnknownTypeName_ShouldReportAnEntry()
    {
        string project = CreateProject("UnknownVariableType");
        AddComponent(project, "Holder", "Container",
            Default(Variable("Blorp", "Label.Whatever", "string", "x")) + Instance("Label", "Text"));

        Check(project).Entries.ShouldContain(e => e.Element == "Holder");
    }

    [Fact]
    public void Check_DuplicateStateNamesInOneComponent_ShouldReportError()
    {
        string project = CreateProject("DuplicateStates");
        AddComponent(project, "Echo", "Container", Default() + Default());

        Check(project).ExitCode.ShouldBe(1);
    }

    [Fact]
    public void Check_SameVariableTwiceInOneState_ShouldReportAnEntry()
    {
        string project = CreateProject("DuplicateVariable");
        AddComponent(project, "Holder", "Container",
            Default(Variable("float", "Width", "float", "1"), Variable("float", "Width", "float", "2")));

        Check(project).Entries.ShouldContain(e => e.Element == "Holder");
    }

    // check overflows the stack: FontReferenceCollector.CollectFontsFromNestedTextInstances recurses without a visited set.
    [Fact]
    public void Check_ComponentInstantiatingItself_ShouldReportErrorInsteadOfCrashing()
    {
        string project = CreateProject("Recursive");
        AddComponent(project, "Matryoshka", "Container", DefaultState() + Instance("Inner", "Matryoshka"));

        Check(project).ExitCode.ShouldBe(1);
    }}
