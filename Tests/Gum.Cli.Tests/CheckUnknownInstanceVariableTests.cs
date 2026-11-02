using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// #7210: a variable set on an instance whose type does not have it (GUM0029).
/// </summary>
public class CheckUnknownInstanceVariableTests : CheckHandWrittenFileHarness
{
    [Fact]
    public void Check_VariableTheInstanceTypeLacks_ShouldReportGum0029Warning()
    {
        string project = CreateProject("UnknownInstanceVariable");
        AddComponent(project, "Holder", "Container",
            DefaultState(Variable("float", "Label.NotAVariable", "float", "1")) + Instance("Label", "Text"));

        CheckRun run = Check(project);

        CheckEntry entry = run.Entries.ShouldHaveSingleItem();
        entry.Code.ShouldBe("GUM0029");
        entry.Severity.ShouldBe("Warning");
        entry.Element.ShouldBe("Holder");
        entry.Message.ShouldContain("Label.NotAVariable");
        run.ExitCode.ShouldBe(0);
    }

    [Fact]
    public void Check_VariableTheInstanceTypeHas_ShouldNotReportGum0029()
    {
        string project = CreateProject("KnownInstanceVariable");
        AddComponent(project, "Holder", "Container",
            DefaultState(Variable("string", "Label.Text", "string", "hi")) + Instance("Label", "Text"));

        Check(project).Entries.ShouldBeEmpty();
    }
}
