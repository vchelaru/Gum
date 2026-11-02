using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// What <c>gumcli check</c> reports for hand-written files: bad base types, a dangling
/// parent, broken or empty files and the output shapes (#7059).
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

    [Fact]
    public void Check_CircularComponentBaseTypes_ShouldReportGum0014ForBoth()
    {
        string project = CreateProject("Circular");
        AddComponent(project, "A", "B", DefaultState());
        AddComponent(project, "B", "A", DefaultState());

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.Where(e => e.Code == "GUM0014").Select(e => e.Element).ShouldBe(new[] { "A", "B" }, ignoreOrder: true);
    }

    [Fact]
    public void Check_ComponentThatIsItsOwnBase_ShouldReportGum0014()
    {
        string project = CreateProject("SelfBase");
        AddComponent(project, "Loop", "Loop", DefaultState());

        Check(project).Entries.ShouldContain(e => e.Code == "GUM0014" && e.Element == "Loop");
    }

    [Fact]
    public void Check_InstanceWithEmptyBaseType_ShouldReportError()
    {
        string project = CreateProject("EmptyInstanceBase");
        AddComponent(project, "Holder", "Container", DefaultState() + "<Instance><Name>Child</Name></Instance>");

        Check(project).ExitCode.ShouldBe(1);
    }

    [Fact]
    public void Check_ReferencedComponentFileMissing_ShouldReportErrorNamingTheComponentAndExit1()
    {
        string project = CreateProject("MissingFile");
        AddReference(project, """  <ComponentReference Name="Ghost" />""");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Ghost");
    }

    [Fact]
    public void Check_ComponentFileWithMalformedXml_ShouldReportErrorNamingTheComponent()
    {
        string project = CreateProject("MalformedElement");
        WriteFile(project, "Components", "Broken.gucx", "<ComponentSave><Name>Broken</Name><BaseType>Container</ComponentSave>");
        AddReference(project, """  <ComponentReference Name="Broken" />""");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Broken");
    }

    [Fact]
    public void Check_MalformedProjectFile_ShouldExit2AndExplainOnStandardError()
    {
        string project = CreateProject("MalformedProject");
        File.WriteAllText(project, "<GumProjectSave><ScreenReferences>");

        CliTestHelper result = CliTestHelper.Run("check", project);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldNotBeEmpty();
    }

    [Fact]
    public void Check_EmptyProjectFile_ShouldExit2()
    {
        string project = CreateProject("EmptyProject");
        File.WriteAllText(project, "");

        CliTestHelper.Run("check", project).ExitCode.ShouldBe(2);
    }

    [Fact]
    public void Check_WrongStateWrapperElement_ShouldReportErrorNamingTheFix()
    {
        string project = CreateProject("WrongWrapper");
        AddComponent(project, "Wrapped", "Container", "<States><State><Name>Default</Name></State></States>");

        CheckRun run = Check(project);

        run.ExitCode.ShouldBe(1);
        run.Entries.ShouldContain(e => e.Element == "Wrapped" && e.Message.Contains("<State>"));
    }

    [Fact]
    public void Check_ErrorsInHumanReadableOutput_ShouldNameTheElementAndTheSeverity()
    {
        string project = CreateProject("HumanOutput");
        AddComponent(project, "Lost", "DoesNotExist", DefaultState());

        CliTestHelper result = CliTestHelper.Run("check", project);

        result.StandardOutput.ShouldContain("error: Lost:");
    }

    [Fact]
    public void Check_JsonOutput_ShouldNotWriteAnExceptionToStandardError()
    {
        string project = CreateProject("JsonStderr");
        AddComponent(project, "Lost", "DoesNotExist", DefaultState());

        Check(project).StandardError.ShouldNotContain("Exception");
    }

    [Fact]
    public void Check_ElementWithTwoProblems_ShouldReportBoth()
    {
        string project = CreateProject("TwoProblems");
        AddComponent(project, "Lost", "DoesNotExist", DefaultState() + Instance("Child", "Gizmo"));

        CheckRun run = Check(project);

        run.Entries.Count(e => e.Element == "Lost").ShouldBeGreaterThanOrEqualTo(2);
    }
}
