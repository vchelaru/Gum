using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// Hand-written files that gumcli check must report (#7119 to #7124).
/// </summary>
public class CheckHandWrittenGapTests : CheckHandWrittenFileHarness
{
    private static string Default(params string[] variables) => DefaultState(variables);

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

    [Fact]
    public void Codegen_ElementWithDuplicateInstanceNames_ShouldBeSkippedWithExitCode1()
    {
        string project = CreateProject("DuplicateInstancesCodegen");
        AddComponent(project, "Twins", "Container", Default() + Instance("Child", "Text") + Instance("Child", "Sprite"));

        CodegenResult(project).ExitCode.ShouldBe(1);
        Directory.GetFiles(Path.GetDirectoryName(project)!, "Twins.Generated.cs", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Fact]
    public void Codegen_ElementReferencedTwice_ShouldBeSkippedWithExitCode1()
    {
        string project = CreateProject("DuplicateReferenceCodegen");
        AddComponent(project, "Twin", "Container", Default());
        AddReference(project, """  <ComponentReference Name="Twin" />""");

        CodegenResult(project).ExitCode.ShouldBe(1);
        Directory.GetFiles(Path.GetDirectoryName(project)!, "Twin.Generated.cs", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    private static CliTestHelper CodegenResult(string project)
    {
        string directory = Path.GetDirectoryName(project)!;
        File.WriteAllText(Path.Combine(directory, "Game.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        CliTestHelper.Run("codegen-init", project);
        return CliTestHelper.Run("codegen", project);
    }
}
