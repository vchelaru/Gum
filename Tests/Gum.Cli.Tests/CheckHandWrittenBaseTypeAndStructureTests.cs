using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// What <c>gumcli check</c> reports for hand-written element files with an unknown base type, a dangling
/// parent or an unusual file layout (#7059).
/// </summary>
public class CheckHandWrittenBaseTypeAndStructureTests : CheckHandWrittenFileHarness
{
    [Fact]
    public void Check_UnknownComponentBaseType_ShouldReportErrorNamingTheBaseAndExit1()
    {
        string project = CreateProject("UnknownBase");
        AddComponent(project, "Lost", "DoesNotExist", DefaultState());

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Lost" && e.Message.Contains("DoesNotExist"));
    }

    [Fact]
    public void Check_UnknownInstanceBaseType_ShouldReportErrorNamingInstanceAndType()
    {
        string project = CreateProject("UnknownInstanceBase");
        AddComponent(project, "Holder", "Container", DefaultState() + Instance("Child", "Gizmo"));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Holder" && e.Message.Contains("Child") && e.Message.Contains("Gizmo"));
    }

    [Fact]
    public void Check_ScreenWithUnknownBaseType_ShouldReportError()
    {
        string project = CreateProject("ScreenUnknownBase");
        AddScreen(project, "Menu", "NotAScreen", DefaultState());

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Menu" && e.Message.Contains("NotAScreen"));
    }

    [Fact]
    public void Check_ParentVariablePointingAtMissingInstance_ShouldReportError()
    {
        string project = CreateProject("MissingParent");
        AddComponent(project, "Holder", "Container",
            DefaultState(Variable("string", "Child.Parent", "string", "NoSuchParent")) + Instance("Child", "Text"));

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Holder" && e.Message.Contains("NoSuchParent"));
    }

    [Fact]
    public void Check_ElementNameDiffersFromItsFileName_ShouldReportAnEntryForThatElement()
    {
        string project = CreateProject("NameMismatch");
        WriteFile(project, "Components", "FileName.gucx", ElementXml("ComponentSave", "InsideName", "Container", DefaultState()));
        AddReference(project, """  <ComponentReference Name="FileName" />""");

        CheckRun run = Check(project);

        run.Entries.ShouldContain(e => e.Message.Contains("InsideName") || e.Element.Contains("InsideName"));
    }

    [Fact]
    public void Check_ProjectPathThatIsADirectory_ShouldExit2()
    {
        CliTestHelper.Run("check", TempDirectory).ExitCode.ShouldBe(2);
    }

    [Fact]
    public void Check_ComponentWithoutDefaultState_ShouldNotCrashAndShouldExitWith0Or1()
    {
        string project = CreateProject("NoDefaultState");
        AddComponent(project, "Bare", "Container", "");

        CheckRun run = Check(project);

        run.StandardError.ShouldNotContain("Exception");
        run.ExitCode.ShouldBeOneOf(0, 1);
    }
}
